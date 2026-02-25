using System;
using System.Collections.Generic;
using Godot;
using PictoMino.Core;
using PictoMino.Input;
using PictoMino.View;
using PictoMino.View.Effects;
using static PictoMino.Core.BoardData;

namespace PictoMino;

/// <summary>
/// 游戏状态枚举。
/// </summary>
public enum GameState
{
	Playing,
	Paused,
	Win
}

/// <summary>
/// 游戏场景控制器。专注于单个谜题的游戏流程。
/// </summary>
public partial class GameController : Node
{
	private BoardView? _boardView;
	private InputDirector? _inputDirector;
	private GhostHand? _ghostHand;
	private PaletteView? _paletteView;
	private WinOverlay? _winOverlay;
	private PauseMenu? _pauseMenu;

	/// <summary>默认棋盘行数</summary>
	[Export] public int DefaultRows { get; set; } = 10;

	/// <summary>默认棋盘列数</summary>
	[Export] public int DefaultCols { get; set; } = 10;

	private BoardData? _boardData;
	private PaletteData? _paletteData;
	private LevelData? _currentLevel;
	private ShapeData? _selectedShape;
	private int _nextShapeId = 1;
	private GameState _gameState = GameState.Playing;
	private float _levelElapsedTime;

	private readonly Dictionary<int, int> _placedShapes = new();

	/// <summary>当前棋盘数据</summary>
	public BoardData? BoardData => _boardData;

	/// <summary>当前 Palette 数据</summary>
	public PaletteData? PaletteData => _paletteData;

	/// <summary>当前游戏状态</summary>
	public GameState State => _gameState;

	/// <summary>当前关卡已用时间</summary>
	public float LevelElapsedTime => _levelElapsedTime;

	/// <summary>当前选中的形状</summary>
	public ShapeData? SelectedShape
	{
		get => _selectedShape;
		set
		{
			_selectedShape = value;
			if (_ghostHand != null)
			{
				_ghostHand.CurrentShape = value;
			}
		}
	}

	/// <summary>当胜利条件达成时触发</summary>
	public event Action? OnWin;

	/// <summary>当游戏状态变化时触发</summary>
	public event Action<GameState>? OnStateChanged;

	public override void _Ready()
	{
		ResolveNodeReferences();
		ValidateExports();
		ConnectSignals();

		GetTree().Root.SizeChanged += OnViewportSizeChanged;

		// 从 GameSession 获取待加载的关卡
		var pendingLevel = GameSession.Instance?.PendingLevel;
		if (pendingLevel != null)
		{
			LoadLevel(pendingLevel);
		}
		else
		{
			GD.PrintErr("GameController: No pending level to load.");
		}
	}

	public override void _ExitTree()
	{
		GetTree().Root.SizeChanged -= OnViewportSizeChanged;
	}

	private void OnViewportSizeChanged()
	{
		UpdateLayout();
	}

	private void UpdateLayout()
	{
		if (_boardView == null || _boardData == null) return;

		var viewportSize = GetViewport().GetVisibleRect().Size;

		float paletteWidth = 180;
		float margin = 20;

		float availableWidth = viewportSize.X - paletteWidth - margin * 3;
		float availableHeight = viewportSize.Y - margin * 2;

		int cellSizeByWidth = (int)(availableWidth / (_boardData.Cols + 2));
		int cellSizeByHeight = (int)(availableHeight / (_boardData.Rows + 2));
		int newCellSize = Mathf.Max(Mathf.Min(cellSizeByWidth, cellSizeByHeight), 24);

		_boardView.CellSize = newCellSize;

		var boardOffset = _boardView.BoardOffset;
		float boardTotalWidth = boardOffset.X + _boardData.Cols * newCellSize;
		float boardTotalHeight = boardOffset.Y + _boardData.Rows * newCellSize;

		float boardX = paletteWidth + margin * 2 + (availableWidth - boardTotalWidth) / 2;
		float boardY = margin + (availableHeight - boardTotalHeight) / 2;

		boardX = Mathf.Max(boardX, paletteWidth + margin * 2);
		boardY = Mathf.Max(boardY, margin);

		_boardView.Position = new Vector2(boardX, boardY);
		_boardView.QueueRedraw();

		if (_paletteView != null)
		{
			_paletteView.Position = new Vector2(margin, margin);
			_paletteView.Size = new Vector2(paletteWidth, viewportSize.Y - margin * 2);
		}

		_ghostHand?.QueueRedraw();
	}

	private bool _escPressedLastFrame = false;

	public override void _Process(double delta)
	{
		if (_gameState == GameState.Playing)
		{
			_levelElapsedTime += (float)delta;
			
			if (Godot.Input.IsActionJustPressed("pause_game"))
			{
				ShowPauseMenu();
			}
		}
	}

	/// <summary>
	/// 显示暂停菜单。
	/// </summary>
	public void ShowPauseMenu()
	{
		if (_gameState != GameState.Playing) return;
		
		SetGameState(GameState.Paused);
		_pauseMenu?.ShowMenu();
	}

	/// <summary>
	/// 隐藏暂停菜单并继续游戏。
	/// </summary>
	public void HidePauseMenu()
	{
		_pauseMenu?.HideMenu();
		SetGameState(GameState.Playing);
	}

	/// <summary>
	/// 加载指定关卡。
	/// </summary>
	public void LoadLevel(LevelData level)
	{
		_currentLevel = level;
		_placedShapes.Clear();
		_nextShapeId = 1;

		InitializeGame(level.Rows, level.Cols, level.Target);

		_paletteData = new PaletteData(level.Shapes);
		_paletteData.OnSelectionChanged += OnPaletteSelectionChanged;
		_paletteData.OnAllShapesUsed += OnAllShapesUsed;

		if (_paletteView != null)
		{
			_paletteView.PaletteData = _paletteData;
		}

		_levelElapsedTime = 0;
		_paletteData.SelectNext();

		SetGameState(GameState.Playing);
		UpdateLayout();
		
		_inputDirector?.ResetCursorToCenter();
		
		GD.Print($"GameController: Loaded level '{level.Name}'.");
	}

	/// <summary>
	/// 重试当前关卡。
	/// </summary>
	public void RetryLevel()
	{
		if (_currentLevel != null)
		{
			LoadLevel(_currentLevel);
		}
	}

	/// <summary>
	/// 加载下一关。
	/// </summary>
	public void LoadNextLevel()
	{
		if (_currentLevel == null) return;

		var session = GameSession.Instance;
		if (session == null) return;

		var nextId = session.LevelManager.GetNextLevelId(_currentLevel.Id);
		if (nextId != null)
		{
			session.StartLevel(nextId);
		}
	}

	/// <summary>
	/// 返回关卡选择。
	/// </summary>
	public void BackToLevelSelect()
	{
		GameSession.Instance?.BackToLevelSelect();
	}

	private void SetGameState(GameState newState)
	{
		if (_gameState == newState) return;

		_gameState = newState;
		OnStateChanged?.Invoke(newState);
		UpdateUIVisibility();
	}

	private void UpdateUIVisibility()
	{
		bool isPlaying = _gameState == GameState.Playing;

		if (_boardView != null)
			_boardView.Visible = isPlaying || _gameState == GameState.Win;

		if (_paletteView != null)
			_paletteView.Visible = isPlaying;
	}

	private void ResolveNodeReferences()
	{
		_boardView = GetNodeOrNull<BoardView>("%BoardView");
		_inputDirector = GetNodeOrNull<InputDirector>("%InputDirector");
		_ghostHand = GetNodeOrNull<GhostHand>("%GhostHand");
		_paletteView = GetNodeOrNull<PaletteView>("%PaletteView");
		_winOverlay = GetNodeOrNull<WinOverlay>("%WinOverlay");
		_pauseMenu = GetNodeOrNull<PauseMenu>("%PauseMenu");
	}

	public void InitializeGame(int rows = 0, int cols = 0, bool[,]? target = null)
	{
		rows = rows > 0 ? rows : DefaultRows;
		cols = cols > 0 ? cols : DefaultCols;

		_boardData = new BoardData(rows, cols, target);
		_nextShapeId = 1;

		if (_boardView != null)
		{
			_boardView.BoardData = _boardData;
		}

		if (_inputDirector != null)
		{
			_inputDirector.BoardRows = rows;
			_inputDirector.BoardCols = cols;
		}

		GD.Print($"GameController: Initialized {rows}x{cols} board.");
	}

	public bool TryPlaceShape(int col, int row)
	{
		if (_boardData == null || _selectedShape == null || _paletteData == null)
		{
			return false;
		}

		int topLeftRow = row - _selectedShape.AnchorRow;
		int topLeftCol = col - _selectedShape.AnchorCol;

		int shapeId = _nextShapeId;
		int paletteIndex = _paletteData.SelectedIndex;

		bool success = _boardData.TryPlace(_selectedShape, topLeftRow, topLeftCol, shapeId);

		if (success)
		{
			_placedShapes[shapeId] = paletteIndex;
			_nextShapeId++;
			_boardView?.PlayPlacementEffect(shapeId);
		}

		return success;
	}

	public int RemoveShapeAndSelect(int shapeId)
	{
		if (_boardData == null || _paletteData == null) return 0;

		int removed = _boardData.Remove(shapeId);
		if (removed > 0 && _placedShapes.TryGetValue(shapeId, out int paletteIndex))
		{
			_paletteData.MarkAsUnused(paletteIndex);
			_placedShapes.Remove(shapeId);
			_paletteData.Select(paletteIndex);
		}

		return removed;
	}

	public int RemoveShapeAndRestore(int shapeId)
	{
		if (_boardData == null || _paletteData == null) return 0;

		int removed = _boardData.Remove(shapeId);
		if (removed > 0 && _placedShapes.TryGetValue(shapeId, out int paletteIndex))
		{
			_paletteData.MarkAsUnused(paletteIndex);
			_placedShapes.Remove(shapeId);
		}

		return removed;
	}

	public int RemoveShape(int shapeId)
	{
		if (_boardData == null) return 0;
		return _boardData.Remove(shapeId);
	}

	private void ValidateExports()
	{
		if (_boardView == null)
			GD.PrintErr("GameController: BoardView not assigned.");
		if (_inputDirector == null)
			GD.PrintErr("GameController: InputDirector not assigned.");
		if (_ghostHand == null)
			GD.PrintErr("GameController: GhostHand not assigned.");
	}

	private void ConnectSignals()
	{
		if (_inputDirector != null)
		{
			_inputDirector.OnGhostPositionChanged += OnGhostPositionChanged;
			_inputDirector.OnInteract += OnInteract;
			_inputDirector.OnCancel += OnCancel;
			_inputDirector.OnRotateClockwise += OnRotateClockwise;
			_inputDirector.OnRotateCounterClockwise += OnRotateCounterClockwise;
			_inputDirector.OnSelectNextShape += OnSelectNextShape;
			_inputDirector.OnSelectPreviousShape += OnSelectPreviousShape;
		}

		if (_paletteView != null)
		{
			_paletteView.OnShapeSelected += OnPaletteShapeSelected;
		}

		if (_winOverlay != null)
		{
			_winOverlay.OnNextLevel += OnWinNextLevel;
			_winOverlay.OnRetry += OnWinRetry;
			_winOverlay.OnBackToMenu += OnWinBackToMenu;
		}

		if (_pauseMenu != null)
		{
			_pauseMenu.OnResume += OnPauseResume;
			_pauseMenu.OnBackToLevelSelect += OnPauseBackToLevelSelect;
			_pauseMenu.OnBackToTitle += OnPauseBackToTitle;
			_pauseMenu.OnExit += OnPauseExit;
		}
	}

	private void OnPaletteSelectionChanged(int oldIndex, int newIndex)
	{
		SelectedShape = _paletteData?.SelectedShape;
	}

	private void OnAllShapesUsed()
	{
		if (_boardData?.CheckWinCondition() == true)
		{
			HandleWin();
		}
	}

	private void OnPaletteShapeSelected(ShapeData? shape)
	{
		SelectedShape = shape;
	}

	private void HandleWin()
	{
		SetGameState(GameState.Win);

		var session = GameSession.Instance;
		if (_currentLevel != null && session != null)
		{
			var oldProgress = session.LevelManager.GetProgress(_currentLevel.Id);
			session.LevelManager.UpdateProgress(_currentLevel.Id, true, _levelElapsedTime);

			bool isNewRecord = oldProgress.BestTime == 0 || _levelElapsedTime < oldProgress.BestTime;
			bool hasNext = session.LevelManager.GetNextLevelId(_currentLevel.Id) != null;

			_winOverlay?.ShowWin(_levelElapsedTime, oldProgress.BestTime, isNewRecord, hasNext);
		}

		OnWin?.Invoke();
		GD.Print("GameController: Win!");
	}

	private void OnWinNextLevel()
	{
		_winOverlay?.HideOverlay();
		LoadNextLevel();
	}

	private void OnWinRetry()
	{
		_winOverlay?.HideOverlay();
		RetryLevel();
	}

	private void OnWinBackToMenu()
	{
		_winOverlay?.HideOverlay();
		BackToLevelSelect();
	}

	private void OnPauseResume()
	{
		HidePauseMenu();
	}

	private void OnPauseBackToLevelSelect()
	{
		_pauseMenu?.HideMenu();
		BackToLevelSelect();
	}

	private void OnPauseBackToTitle()
	{
		_pauseMenu?.HideMenu();
		GameSession.Instance?.GoToTitle();
	}

	private void OnPauseExit()
	{
		GetTree().Quit();
	}

	private void OnGhostPositionChanged(Vector2I gridPos)
	{
		if (_ghostHand == null || _boardData == null) return;

		_ghostHand.GridPosition = gridPos;

		if (_selectedShape != null)
		{
			UpdateGhostPlacementState(gridPos.X, gridPos.Y);
		}
	}

	private void OnInteract(Vector2I gridPos)
	{
		if (_gameState != GameState.Playing) return;
		if (_boardData == null) return;

		if (!_boardData.IsInBounds(gridPos.Y, gridPos.X))
		{
			return;
		}

		int existingShapeId = _boardData.GetCell(gridPos.Y, gridPos.X);

		if (existingShapeId != 0)
		{
			RemoveShapeAndSelect(existingShapeId);
		}
		else if (_selectedShape != null && _paletteData != null)
		{
			bool placed = TryForcePlaceShape(gridPos.X, gridPos.Y);
			if (placed)
			{
				_paletteData.MarkSelectedAsUsed();
				_paletteData.SelectNext();
			}
		}
	}

	private void OnCancel()
	{
		// 取消操作只取消选择，不打开暂停菜单
		if (_gameState != GameState.Playing) return;

		_paletteData?.Deselect();
		SelectedShape = null;
	}

	private void OnRotateClockwise()
	{
		if (_gameState != GameState.Playing || _selectedShape == null) return;

		_ghostHand?.RotateClockwise();
		_selectedShape = _ghostHand?.CurrentShape;

		if (_ghostHand?.GridPosition != null)
		{
			var pos = _ghostHand.GridPosition.Value;
			UpdateGhostPlacementState(pos.X, pos.Y);
		}
	}

	private void OnRotateCounterClockwise()
	{
		if (_gameState != GameState.Playing || _selectedShape == null) return;

		_ghostHand?.RotateCounterClockwise();
		_selectedShape = _ghostHand?.CurrentShape;

		if (_ghostHand?.GridPosition != null)
		{
			var pos = _ghostHand.GridPosition.Value;
			UpdateGhostPlacementState(pos.X, pos.Y);
		}
	}

	private void OnSelectNextShape()
	{
		if (_gameState != GameState.Playing || _paletteData == null) return;
		_paletteData.SelectNext();
	}

	private void OnSelectPreviousShape()
	{
		if (_gameState != GameState.Playing || _paletteData == null) return;
		_paletteData.SelectPrevious();
	}

	private void UpdateGhostPlacementState(int col, int row)
	{
		if (_boardData == null || _selectedShape == null || _ghostHand == null) return;

		int topLeftRow = row - _selectedShape.AnchorRow;
		int topLeftCol = col - _selectedShape.AnchorCol;

		var status = _boardData.CheckPlacement(_selectedShape, topLeftRow, topLeftCol);

		_ghostHand.PlacementState = status switch
		{
			PlacementStatus.Valid => GhostPlacementState.Valid,
			PlacementStatus.Overlapping => GhostPlacementState.Warning,
			PlacementStatus.OutOfBounds => GhostPlacementState.Invalid,
			_ => GhostPlacementState.Invalid
		};
	}

	private bool TryForcePlaceShape(int col, int row)
	{
		if (_boardData == null || _selectedShape == null || _paletteData == null)
		{
			return false;
		}

		int topLeftRow = row - _selectedShape.AnchorRow;
		int topLeftCol = col - _selectedShape.AnchorCol;

		int shapeId = _nextShapeId;
		int paletteIndex = _paletteData.SelectedIndex;

		var overwrittenIds = _boardData.ForcePlace(_selectedShape, topLeftRow, topLeftCol, shapeId);

		if (overwrittenIds == null)
		{
			return false;
		}

		foreach (int overwrittenId in overwrittenIds)
		{
			if (_placedShapes.TryGetValue(overwrittenId, out int overwrittenPaletteIndex))
			{
				_paletteData.MarkAsUnused(overwrittenPaletteIndex);
				_placedShapes.Remove(overwrittenId);
			}
		}

		_placedShapes[shapeId] = paletteIndex;
		_nextShapeId++;

		_boardView?.PlayPlacementEffect(shapeId);

		return true;
	}
}
