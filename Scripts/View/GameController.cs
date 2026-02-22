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
    Menu,
    LevelSelect,
    Playing,
    Paused,
    Win
}

/// <summary>
/// 游戏主控制器。连接 Model、View、Input 各层。
/// </summary>
public partial class GameController : Node
{
    private BoardView? _boardView;
    private InputDirector? _inputDirector;
    private GhostHand? _ghostHand;
    private PaletteView? _paletteView;
    private WinOverlay? _winOverlay;
    private LevelSelectMenu? _levelSelectMenu;

    /// <summary>默认棋盘行数</summary>
    [Export] public int DefaultRows { get; set; } = 10;

    /// <summary>默认棋盘列数</summary>
    [Export] public int DefaultCols { get; set; } = 10;

    private BoardData? _boardData;
    private PaletteData? _paletteData;
    private LevelManager _levelManager = new();
    private LevelData? _currentLevel;
    private ShapeData? _selectedShape;
    private int _nextShapeId = 1;
    private GameState _gameState = GameState.Menu;
    private float _levelStartTime;
    private float _levelElapsedTime;

    // 跟踪放置的形状：shapeId -> paletteIndex
    private readonly Dictionary<int, int> _placedShapes = new();

    /// <summary>
    /// 当前棋盘数据。
    /// </summary>
    public BoardData? BoardData => _boardData;

    /// <summary>
    /// 当前 Palette 数据。
    /// </summary>
    public PaletteData? PaletteData => _paletteData;

    /// <summary>
    /// 当前游戏状态。
    /// </summary>
    public GameState State => _gameState;

    /// <summary>
    /// 当前关卡已用时间。
    /// </summary>
    public float LevelElapsedTime => _levelElapsedTime;

    /// <summary>
    /// 当前选中的形状。
    /// </summary>
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

    /// <summary>
    /// 当胜利条件达成时触发。
    /// </summary>
    public event Action? OnWin;

    /// <summary>
    /// 当游戏状态变化时触发。
    /// </summary>
    public event Action<GameState>? OnStateChanged;

    public override void _Ready()
    {
        ResolveNodeReferences();
        ValidateExports();
        InitializeLevelManager();
        ConnectSignals();
        
        // 直接加载教程第一关进行游戏
        var firstLevel = _levelManager.GetLevel("tutorial_01");
        if (firstLevel != null)
        {
            LoadLevel(firstLevel);
        }
        else
        {
            // 回退到测试模式
            InitializeGame();
            SelectTestShape();
        }
    }

    public override void _Process(double delta)
    {
        // 更新关卡计时
        if (_gameState == GameState.Playing)
        {
            _levelElapsedTime += (float)delta;
        }
    }

    /// <summary>
    /// 初始化关卡管理器。
    /// </summary>
    private void InitializeLevelManager()
    {
        _levelManager = new LevelManager();
        _levelManager.AddChapter(LevelManager.CreateTutorialChapter());
        GD.Print($"GameController: Loaded {_levelManager.TotalLevelCount} levels.");
    }

    /// <summary>
    /// 加载指定关卡。
    /// </summary>
    public void LoadLevel(LevelData level)
    {
        _currentLevel = level;
        
        // 清理上一关的状态
        _placedShapes.Clear();
        _nextShapeId = 1;
        
        // 初始化棋盘
        InitializeGame(level.Rows, level.Cols, level.Target);
        
        // 初始化 Palette
        _paletteData = new PaletteData(level.Shapes);
        _paletteData.OnSelectionChanged += OnPaletteSelectionChanged;
        _paletteData.OnAllShapesUsed += OnAllShapesUsed;
        
        if (_paletteView != null)
        {
            _paletteView.PaletteData = _paletteData;
        }
        
        // 重置计时
        _levelStartTime = (float)Time.GetTicksMsec() / 1000f;
        _levelElapsedTime = 0;
        
        // 自动选择第一个形状
        _paletteData.SelectNext();
        
        SetGameState(GameState.Playing);
        GD.Print($"GameController: Loaded level '{level.Name}'.");
    }

    /// <summary>
    /// 加载指定 ID 的关卡。
    /// </summary>
    public void LoadLevel(string levelId)
    {
        var level = _levelManager.GetLevel(levelId);
        if (level != null)
        {
            LoadLevel(level);
        }
        else
        {
            GD.PrintErr($"GameController: Level '{levelId}' not found.");
        }
    }

    /// <summary>
    /// 加载下一关。
    /// </summary>
    public void LoadNextLevel()
    {
        if (_currentLevel == null) return;
        
        var nextId = _levelManager.GetNextLevelId(_currentLevel.Id);
        if (nextId != null)
        {
            LoadLevel(nextId);
        }
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
    /// 显示关卡选择菜单。
    /// </summary>
    public void ShowLevelSelect()
    {
        SetGameState(GameState.LevelSelect);
        _levelSelectMenu?.ShowMenu();
    }

    /// <summary>
    /// 设置游戏状态。
    /// </summary>
    private void SetGameState(GameState newState)
    {
        if (_gameState == newState) return;
        
        _gameState = newState;
        OnStateChanged?.Invoke(newState);
        
        // 更新 UI 可见性
        UpdateUIVisibility();
    }

    /// <summary>
    /// 更新 UI 可见性。
    /// </summary>
    private void UpdateUIVisibility()
    {
        bool isPlaying = _gameState == GameState.Playing;
        
        if (_boardView != null)
            _boardView.Visible = isPlaying || _gameState == GameState.Win;
        
        if (_paletteView != null)
            _paletteView.Visible = isPlaying;
    }

    /// <summary>
    /// 设置一个测试用的 L 形状。
    /// </summary>
    public void SelectTestShape()
    {
        // L 形状
        var lShape = new bool[,]
        {
            { true, false },
            { true, false },
            { true, true }
        };
        SelectedShape = new ShapeData(lShape);
        GD.Print("GameController: Test L-shape selected.");
    }

    /// <summary>
    /// 解析节点引用。
    /// </summary>
    private void ResolveNodeReferences()
    {
        _boardView = GetNodeOrNull<BoardView>("%BoardView");
        _inputDirector = GetNodeOrNull<InputDirector>("%InputDirector");
        _ghostHand = GetNodeOrNull<GhostHand>("%GhostHand");
        _paletteView = GetNodeOrNull<PaletteView>("%PaletteView");
        _winOverlay = GetNodeOrNull<WinOverlay>("%WinOverlay");
        _levelSelectMenu = GetNodeOrNull<LevelSelectMenu>("%LevelSelectMenu");
    }

    /// <summary>
    /// 初始化新游戏。
    /// </summary>
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

    /// <summary>
    /// 尝试在指定位置放置当前选中的形状。
    /// 注意：col/row 是锚点位置，需要转换为左上角。
    /// </summary>
    public bool TryPlaceShape(int col, int row)
    {
        if (_boardData == null || _selectedShape == null || _paletteData == null)
        {
            GD.Print("GameController: No board or shape selected.");
            return false;
        }

        // 将锚点位置转换为左上角位置
        int topLeftRow = row - _selectedShape.AnchorRow;
        int topLeftCol = col - _selectedShape.AnchorCol;

        int shapeId = _nextShapeId;
        int paletteIndex = _paletteData.SelectedIndex;
        
        bool success = _boardData.TryPlace(_selectedShape, topLeftRow, topLeftCol, shapeId);

        if (success)
        {
            // 记录形状映射
            _placedShapes[shapeId] = paletteIndex;
            _nextShapeId++;
            GD.Print($"GameController: Placed shape at anchor ({col}, {row}), top-left ({topLeftCol}, {topLeftRow}) with ID {shapeId}, palette index {paletteIndex}.");

            // 胜利检查现在由 OnAllShapesUsed 处理
        }
        else
        {
            GD.Print($"GameController: Cannot place shape at ({col}, {row}).");
        }

        return success;
    }

    /// <summary>
    /// 移除指定 ID 的形状，并直接选中该形状进入放置模式。
    /// </summary>
    /// <returns>移除的格子数。</returns>
    public int RemoveShapeAndSelect(int shapeId)
    {
        if (_boardData == null || _paletteData == null) return 0;
        
        int removed = _boardData.Remove(shapeId);
        if (removed > 0 && _placedShapes.TryGetValue(shapeId, out int paletteIndex))
        {
            _paletteData.MarkAsUnused(paletteIndex);
            _placedShapes.Remove(shapeId);
            
            // 直接选中该形状进入放置模式
            _paletteData.Select(paletteIndex);
            
            GD.Print($"GameController: Removed shape ID {shapeId}, selected palette index {paletteIndex} for placement.");
        }
        
        return removed;
    }

    /// <summary>
    /// 移除指定 ID 的形状，并将其恢复到 Palette。
    /// </summary>
    /// <returns>移除的格子数。</returns>
    public int RemoveShapeAndRestore(int shapeId)
    {
        if (_boardData == null || _paletteData == null) return 0;
        
        int removed = _boardData.Remove(shapeId);
        if (removed > 0 && _placedShapes.TryGetValue(shapeId, out int paletteIndex))
        {
            _paletteData.MarkAsUnused(paletteIndex);
            _placedShapes.Remove(shapeId);
            GD.Print($"GameController: Removed shape ID {shapeId}, restored palette index {paletteIndex}.");
        }
        
        return removed;
    }

    /// <summary>
    /// 移除指定 ID 的形状。
    /// </summary>
    public int RemoveShape(int shapeId)
    {
        if (_boardData == null) return 0;
        return _boardData.Remove(shapeId);
    }

    /// <summary>
    /// 验证 Export 字段是否已分配。
    /// </summary>
    private void ValidateExports()
    {
        if (_boardView == null)
            GD.PrintErr("GameController: BoardView not assigned.");
        if (_inputDirector == null)
            GD.PrintErr("GameController: InputDirector not assigned.");
        if (_ghostHand == null)
            GD.PrintErr("GameController: GhostHand not assigned.");
    }

    /// <summary>
    /// 连接输入信号。
    /// </summary>
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

        if (_levelSelectMenu != null)
        {
            _levelSelectMenu.LevelManager = _levelManager;
            _levelSelectMenu.OnLevelSelected += OnLevelMenuSelected;
            _levelSelectMenu.OnBack += OnLevelMenuBack;
        }
    }

    /// <summary>
    /// 当 Palette 选择变化时。
    /// </summary>
    private void OnPaletteSelectionChanged(int oldIndex, int newIndex)
    {
        SelectedShape = _paletteData?.SelectedShape;
    }

    /// <summary>
    /// 当 Palette 所有形状用完时。
    /// </summary>
    private void OnAllShapesUsed()
    {
        // 检查胜利条件
        if (_boardData?.CheckWinCondition() == true)
        {
            HandleWin();
        }
    }

    /// <summary>
    /// 当 PaletteView 选择形状时。
    /// </summary>
    private void OnPaletteShapeSelected(ShapeData? shape)
    {
        SelectedShape = shape;
    }

    /// <summary>
    /// 处理胜利。
    /// </summary>
    private void HandleWin()
    {
        SetGameState(GameState.Win);
        
        // 更新进度
        if (_currentLevel != null)
        {
            var oldProgress = _levelManager.GetProgress(_currentLevel.Id);
            _levelManager.UpdateProgress(_currentLevel.Id, true, _levelElapsedTime);
            
            bool isNewRecord = oldProgress.BestTime == 0 || _levelElapsedTime < oldProgress.BestTime;
            bool hasNext = _levelManager.GetNextLevelId(_currentLevel.Id) != null;
            
            _winOverlay?.ShowWin(_levelElapsedTime, oldProgress.BestTime, isNewRecord, hasNext);
        }
        
        OnWin?.Invoke();
        GD.Print("GameController: Win!");
    }

    /// <summary>
    /// 胜利界面 - 下一关。
    /// </summary>
    private void OnWinNextLevel()
    {
        _winOverlay?.HideOverlay();
        LoadNextLevel();
    }

    /// <summary>
    /// 胜利界面 - 重试。
    /// </summary>
    private void OnWinRetry()
    {
        _winOverlay?.HideOverlay();
        RetryLevel();
    }

    /// <summary>
    /// 胜利界面 - 返回菜单。
    /// </summary>
    private void OnWinBackToMenu()
    {
        _winOverlay?.HideOverlay();
        ShowLevelSelect();
    }

    /// <summary>
    /// 关卡菜单 - 选择关卡。
    /// </summary>
    private void OnLevelMenuSelected(string levelId)
    {
        _levelSelectMenu?.HideMenu();
        LoadLevel(levelId);
    }

    /// <summary>
    /// 关卡菜单 - 返回。
    /// </summary>
    private void OnLevelMenuBack()
    {
        _levelSelectMenu?.HideMenu();
        // 返回主菜单或继续游戏
        if (_currentLevel != null)
        {
            SetGameState(GameState.Playing);
        }
    }

    /// <summary>
    /// 当 Ghost 位置变化时的处理。
    /// </summary>
    private void OnGhostPositionChanged(Vector2I gridPos)
    {
        if (_ghostHand == null || _boardData == null) return;

        _ghostHand.GridPosition = gridPos;

        // 验证放置有效性
        if (_selectedShape != null)
        {
            UpdateGhostPlacementState(gridPos.X, gridPos.Y);
        }
    }

    /// <summary>
    /// 当交互操作时的处理。
    /// </summary>
    private void OnInteract(Vector2I gridPos)
    {
        if (_gameState != GameState.Playing) return;
        if (_boardData == null) return;
        
        GD.Print($"GameController: Interact at ({gridPos.X}, {gridPos.Y})");
        
        // 检查点击位置是否在棋盘范围内
        if (!_boardData.IsInBounds(gridPos.Y, gridPos.X))
        {
            GD.Print("GameController: Click out of bounds.");
            return;
        }
        
        // 检查点击的格子是否已有形状
        int existingShapeId = _boardData.GetCell(gridPos.Y, gridPos.X);
        
        if (existingShapeId != 0)
        {
            // 点击已填充的格子 - 移除该形状并直接选中进入放置模式
            RemoveShapeAndSelect(existingShapeId);
        }
        else if (_selectedShape != null && _paletteData != null)
        {
            // 点击空格子或其他形状 - 放置形状（允许重叠）
            bool placed = TryForcePlaceShape(gridPos.X, gridPos.Y);
            if (placed)
            {
                // 标记当前形状为已使用
                _paletteData.MarkSelectedAsUsed();
                
                // 自动选择下一个可用形状
                _paletteData.SelectNext();
            }
        }
    }

    /// <summary>
    /// 当取消操作时的处理。
    /// </summary>
    private void OnCancel()
    {
        if (_gameState != GameState.Playing) return;
        
        _paletteData?.Deselect();
        SelectedShape = null;
        GD.Print("GameController: Selection cancelled.");
    }

    /// <summary>
    /// 顺时针旋转当前形状。
    /// </summary>
    private void OnRotateClockwise()
    {
        if (_gameState != GameState.Playing || _selectedShape == null) return;
        
        _ghostHand?.RotateClockwise();
        _selectedShape = _ghostHand?.CurrentShape;
        
        // 更新放置状态
        if (_ghostHand?.GridPosition != null)
        {
            var pos = _ghostHand.GridPosition.Value;
            UpdateGhostPlacementState(pos.X, pos.Y);
        }
        
        GD.Print("GameController: Rotated clockwise.");
    }

    /// <summary>
    /// 逆时针旋转当前形状。
    /// </summary>
    private void OnRotateCounterClockwise()
    {
        if (_gameState != GameState.Playing || _selectedShape == null) return;
        
        _ghostHand?.RotateCounterClockwise();
        _selectedShape = _ghostHand?.CurrentShape;
        
        // 更新放置状态
        if (_ghostHand?.GridPosition != null)
        {
            var pos = _ghostHand.GridPosition.Value;
            UpdateGhostPlacementState(pos.X, pos.Y);
        }
        
        GD.Print("GameController: Rotated counter-clockwise.");
    }

    /// <summary>
    /// 更新 Ghost 的放置状态。
    /// </summary>
    private void UpdateGhostPlacementState(int col, int row)
    {
        if (_boardData == null || _selectedShape == null || _ghostHand == null) return;

        // 将锚点位置转换为左上角位置
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

    /// <summary>
    /// 尝试强制放置形状（允许重叠）。
    /// </summary>
    private bool TryForcePlaceShape(int col, int row)
    {
        if (_boardData == null || _selectedShape == null || _paletteData == null)
        {
            GD.Print("GameController: No board or shape selected.");
            return false;
        }

        // 将锚点位置转换为左上角位置
        int topLeftRow = row - _selectedShape.AnchorRow;
        int topLeftCol = col - _selectedShape.AnchorCol;

        int shapeId = _nextShapeId;
        int paletteIndex = _paletteData.SelectedIndex;
        
        var overwrittenIds = _boardData.ForcePlace(_selectedShape, topLeftRow, topLeftCol, shapeId);

        if (overwrittenIds == null)
        {
            // 超出边界，无法放置
            GD.Print($"GameController: Cannot place shape at ({col}, {row}) - out of bounds.");
            return false;
        }

        // 处理被覆盖的形状
        foreach (int overwrittenId in overwrittenIds)
        {
            if (_placedShapes.TryGetValue(overwrittenId, out int overwrittenPaletteIndex))
            {
                // 恢复被覆盖形状到 Palette
                _paletteData.MarkAsUnused(overwrittenPaletteIndex);
                _placedShapes.Remove(overwrittenId);
                GD.Print($"GameController: Shape ID {overwrittenId} was overwritten, restored to palette.");
            }
        }

        // 记录新放置的形状
        _placedShapes[shapeId] = paletteIndex;
        _nextShapeId++;
        GD.Print($"GameController: Force placed shape at anchor ({col}, {row}) with ID {shapeId}.");

        return true;
    }
}
