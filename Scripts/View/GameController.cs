using System;
using System.Collections.Generic;
using Godot;
using PictoMino.Core;
using PictoMino.Input;
using PictoMino.View;
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

	public override void _Process(double delta)
	{
		if (_gameState == GameState.Playing)
		{
			_levelElapsedTime += (float)delta;
		}
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

		return true;
	}
}
