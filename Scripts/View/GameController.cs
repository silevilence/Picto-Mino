using System;
using Godot;
using PictoMino.Core;
using PictoMino.Input;
using PictoMino.View;

namespace PictoMino;

/// <summary>
/// 游戏主控制器。连接 Model、View、Input 各层。
/// </summary>
public partial class GameController : Node
{
    private BoardView? _boardView;
    private InputDirector? _inputDirector;
    private GhostHand? _ghostHand;

    /// <summary>默认棋盘行数</summary>
    [Export] public int DefaultRows { get; set; } = 10;

    /// <summary>默认棋盘列数</summary>
    [Export] public int DefaultCols { get; set; } = 10;

    private BoardData? _boardData;
    private ShapeData? _selectedShape;
    private int _nextShapeId = 1;

    /// <summary>
    /// 当前棋盘数据。
    /// </summary>
    public BoardData? BoardData => _boardData;

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

    public override void _Ready()
    {
        ResolveNodeReferences();
        ValidateExports();
        InitializeGame();
        ConnectSignals();
        
        // 设置测试形状（L形）
        SelectTestShape();
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
    /// </summary>
    public bool TryPlaceShape(int col, int row)
    {
        if (_boardData == null || _selectedShape == null)
        {
            GD.Print("GameController: No board or shape selected.");
            return false;
        }

        int shapeId = _nextShapeId;
        bool success = _boardData.TryPlace(_selectedShape, row, col, shapeId);

        if (success)
        {
            _nextShapeId++;
            GD.Print($"GameController: Placed shape at ({col}, {row}) with ID {shapeId}.");

            // 检查胜利条件
            if (_boardData.CheckWinCondition())
            {
                GD.Print("GameController: Win condition met!");
                OnWin?.Invoke();
            }
        }
        else
        {
            GD.Print($"GameController: Cannot place shape at ({col}, {row}).");
        }

        return success;
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
        if (_inputDirector == null) return;

        _inputDirector.OnGhostPositionChanged += OnGhostPositionChanged;
        _inputDirector.OnInteract += OnInteract;
        _inputDirector.OnCancel += OnCancel;
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
            bool isValid = CanPlace(gridPos.X, gridPos.Y);
            _ghostHand.IsValidPlacement = isValid;
        }
    }

    /// <summary>
    /// 当交互操作时的处理。
    /// </summary>
    private void OnInteract(Vector2I gridPos)
    {
        GD.Print($"GameController: Interact at ({gridPos.X}, {gridPos.Y})");
        if (_selectedShape != null)
        {
            bool placed = TryPlaceShape(gridPos.X, gridPos.Y);
            if (placed)
            {
                // 放置成功后，重新选择测试形状
                SelectTestShape();
            }
        }
        else
        {
            // 没有形状时，选择测试形状
            SelectTestShape();
        }
    }

    /// <summary>
    /// 当取消操作时的处理。
    /// </summary>
    private void OnCancel()
    {
        SelectedShape = null;
        GD.Print("GameController: Selection cancelled.");
    }

    /// <summary>
    /// 检查是否可以在指定位置放置当前形状。
    /// </summary>
    private bool CanPlace(int col, int row)
    {
        if (_boardData == null || _selectedShape == null) return false;

        for (int r = 0; r < _selectedShape.Rows; r++)
        {
            for (int c = 0; c < _selectedShape.Cols; c++)
            {
                if (!_selectedShape.Matrix[r, c]) continue;

                int boardRow = row + r;
                int boardCol = col + c;

                if (!_boardData.IsInBounds(boardRow, boardCol)) return false;
                if (_boardData.GetCell(boardRow, boardCol) != 0) return false;
            }
        }

        return true;
    }
}
