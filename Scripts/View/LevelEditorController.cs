using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PictoMino.Core;
using PictoMino.Core.Serialization;

namespace PictoMino.View;

/// <summary>
/// 关卡编辑器控制器。
/// </summary>
public partial class LevelEditorController : Control
{
    private const int MaxBoardSize = 25;
    private const int MinBoardSize = 2;
    private const int DefaultBoardSize = 5;

    private int _boardRows = DefaultBoardSize;
    private int _boardCols = DefaultBoardSize;
    private bool[,] _targetPattern = new bool[DefaultBoardSize, DefaultBoardSize];
    private List<ShapeData> _selectedShapes = new();
    private List<string> _selectedShapeIds = new();

    // 自定义形状
    private Dictionary<string, ShapeData> _customShapes = new();
    private Dictionary<string, ShapeFileData> _customShapeFiles = new();
    private int _customShapeCounter = 1;

    // UI Elements
    private Control? _root;
    private SpinBox? _rowsSpinBox;
    private SpinBox? _colsSpinBox;
    private LineEdit? _levelNameEdit;
    private SpinBox? _difficultySpinBox;
    private Control? _boardCanvas;
    private VBoxContainer? _shapeListContainer;
    private VBoxContainer? _selectedShapesContainer;
    private Label? _statusLabel;
    private Button? _checkButton;
    private Button? _autoSelectButton;
    private Button? _exportButton;
    private Button? _importButton;
    private Button? _backButton;
    private Button? _clearBoardButton;
    private Button? _fillBoardButton;
    private FileDialog? _exportDialog;
    private FileDialog? _importDialog;
    private AcceptDialog? _currentMessageDialog;
    private Window? _shapeEditorDialog;

    private BuiltinShapeRegistry? _shapeRegistry;

    public override void _Ready()
    {
        _shapeRegistry = GodotLevelLoader.GetShapeRegistry();
        CreateUI();
        RefreshBoardCanvas();
        RefreshShapeList();
    }

    private void CreateUI()
    {
        _root = new Control();
        _root.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_root);

        var background = new ColorRect { Color = new Color(0.1f, 0.1f, 0.15f, 1f) };
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        _root.AddChild(background);

        var mainMargin = new MarginContainer();
        mainMargin.SetAnchorsPreset(LayoutPreset.FullRect);
        mainMargin.AddThemeConstantOverride("margin_left", 20);
        mainMargin.AddThemeConstantOverride("margin_right", 20);
        mainMargin.AddThemeConstantOverride("margin_top", 20);
        mainMargin.AddThemeConstantOverride("margin_bottom", 20);
        _root.AddChild(mainMargin);

        var mainHBox = new HBoxContainer();
        mainHBox.AddThemeConstantOverride("separation", 20);
        mainMargin.AddChild(mainHBox);

        // Left panel: Settings and shape selection
        var leftPanel = CreateLeftPanel();
        mainHBox.AddChild(leftPanel);

        // Center: Board canvas
        var centerPanel = CreateCenterPanel();
        centerPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mainHBox.AddChild(centerPanel);

        // Right panel: Selected shapes and actions
        var rightPanel = CreateRightPanel();
        mainHBox.AddChild(rightPanel);

        CreateDialogs();
    }

    private VBoxContainer CreateLeftPanel()
    {
        var panel = new VBoxContainer();
        panel.CustomMinimumSize = new Vector2(250, 0);
        panel.AddThemeConstantOverride("separation", 15);

        // Title
        var titleLabel = new Label { Text = "关卡编辑器" };
        titleLabel.AddThemeFontSizeOverride("font_size", 28);
        panel.AddChild(titleLabel);

        // Board size settings
        var sizeGroup = CreateGroupBox("棋盘大小");
        panel.AddChild(sizeGroup);

        var sizeHBox = new HBoxContainer();
        sizeHBox.AddThemeConstantOverride("separation", 10);
        sizeGroup.AddChild(sizeHBox);

        sizeHBox.AddChild(new Label { Text = "行:" });
        _rowsSpinBox = new SpinBox { MinValue = MinBoardSize, MaxValue = MaxBoardSize, Value = _boardRows };
        _rowsSpinBox.ValueChanged += OnBoardSizeChanged;
        sizeHBox.AddChild(_rowsSpinBox);

        sizeHBox.AddChild(new Label { Text = "列:" });
        _colsSpinBox = new SpinBox { MinValue = MinBoardSize, MaxValue = MaxBoardSize, Value = _boardCols };
        _colsSpinBox.ValueChanged += OnBoardSizeChanged;
        sizeHBox.AddChild(_colsSpinBox);

        // Level info
        var infoGroup = CreateGroupBox("关卡信息");
        panel.AddChild(infoGroup);

        var nameHBox = new HBoxContainer();
        nameHBox.AddThemeConstantOverride("separation", 10);
        infoGroup.AddChild(nameHBox);
        nameHBox.AddChild(new Label { Text = "名称:" });
        _levelNameEdit = new LineEdit { Text = "新关卡", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        nameHBox.AddChild(_levelNameEdit);

        var diffHBox = new HBoxContainer();
        diffHBox.AddThemeConstantOverride("separation", 10);
        infoGroup.AddChild(diffHBox);
        diffHBox.AddChild(new Label { Text = "难度:" });
        _difficultySpinBox = new SpinBox { MinValue = 1, MaxValue = 5, Value = 1 };
        diffHBox.AddChild(_difficultySpinBox);

        // Board tools
        var toolsGroup = CreateGroupBox("画板工具");
        panel.AddChild(toolsGroup);

        var toolsHBox = new HBoxContainer();
        toolsHBox.AddThemeConstantOverride("separation", 10);
        toolsGroup.AddChild(toolsHBox);

        _clearBoardButton = new Button { Text = "清空", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _clearBoardButton.Pressed += OnClearBoard;
        toolsHBox.AddChild(_clearBoardButton);

        _fillBoardButton = new Button { Text = "填满", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _fillBoardButton.Pressed += OnFillBoard;
        toolsHBox.AddChild(_fillBoardButton);

        // Available shapes
        var shapesGroup = CreateGroupBox("可用形状 (点击添加)");
        shapesGroup.SizeFlagsVertical = SizeFlags.ExpandFill;
        panel.AddChild(shapesGroup);

        var newShapeButton = new Button { Text = "+ 新建自定义形状" };
        newShapeButton.Pressed += OnNewCustomShape;
        shapesGroup.AddChild(newShapeButton);

        var shapeScroll = new ScrollContainer();
        shapeScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        shapeScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        shapesGroup.AddChild(shapeScroll);

        _shapeListContainer = new VBoxContainer();
        _shapeListContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _shapeListContainer.AddThemeConstantOverride("separation", 5);
        shapeScroll.AddChild(_shapeListContainer);

        return panel;
    }

    private VBoxContainer CreateCenterPanel()
    {
        var panel = new VBoxContainer();
        panel.AddThemeConstantOverride("separation", 10);

        var headerLabel = new Label { Text = "目标图案 (点击切换格子)" };
        headerLabel.AddThemeFontSizeOverride("font_size", 20);
        panel.AddChild(headerLabel);

        _boardCanvas = new Control();
        _boardCanvas.SizeFlagsVertical = SizeFlags.ExpandFill;
        _boardCanvas.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _boardCanvas.GuiInput += OnBoardCanvasInput;
        _boardCanvas.Draw += OnBoardCanvasDraw;
        panel.AddChild(_boardCanvas);

        return panel;
    }

    private VBoxContainer CreateRightPanel()
    {
        var panel = new VBoxContainer();
        panel.CustomMinimumSize = new Vector2(220, 0);
        panel.AddThemeConstantOverride("separation", 15);

        // Selected shapes
        var selectedGroup = CreateGroupBox("已选形状");
        selectedGroup.SizeFlagsVertical = SizeFlags.ExpandFill;
        panel.AddChild(selectedGroup);

        var selectedScroll = new ScrollContainer();
        selectedScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        selectedScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        selectedGroup.AddChild(selectedScroll);

        _selectedShapesContainer = new VBoxContainer();
        _selectedShapesContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _selectedShapesContainer.AddThemeConstantOverride("separation", 5);
        selectedScroll.AddChild(_selectedShapesContainer);

        // Status
        _statusLabel = new Label { Text = "目标格数: 0\n形状格数: 0" };
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        panel.AddChild(_statusLabel);

        // Actions
        var actionsGroup = CreateGroupBox("操作");
        panel.AddChild(actionsGroup);

        _checkButton = new Button { Text = "检查唯一解" };
        _checkButton.Pressed += OnCheckUniqueness;
        actionsGroup.AddChild(_checkButton);

        _autoSelectButton = new Button { Text = "自动选择形状" };
        _autoSelectButton.Pressed += OnAutoSelectShapes;
        actionsGroup.AddChild(_autoSelectButton);

        _exportButton = new Button { Text = "导出 .level 文件" };
        _exportButton.Pressed += OnExport;
        actionsGroup.AddChild(_exportButton);

        _importButton = new Button { Text = "导入关卡" };
        _importButton.Pressed += OnImport;
        actionsGroup.AddChild(_importButton);

        _backButton = new Button { Text = "← 返回标题" };
        _backButton.Pressed += OnBack;
        actionsGroup.AddChild(_backButton);

        return panel;
    }

    private VBoxContainer CreateGroupBox(string title)
    {
        var group = new VBoxContainer();
        group.AddThemeConstantOverride("separation", 8);

        var titleLabel = new Label { Text = title };
        titleLabel.AddThemeFontSizeOverride("font_size", 16);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.9f));
        group.AddChild(titleLabel);

        var separator = new HSeparator();
        group.AddChild(separator);

        return group;
    }

    private void CreateDialogs()
    {
        _exportDialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Title = "导出关卡文件",
            Size = new Vector2I(800, 600),
            Transient = false
        };
        _exportDialog.AddFilter("*.level", "关卡文件");
        _exportDialog.FileSelected += OnExportFileSelected;
        _exportDialog.Canceled += OnDialogCanceled;
        AddChild(_exportDialog);

        _importDialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Title = "导入关卡文件",
            Size = new Vector2I(800, 600),
            Transient = false
        };
        _importDialog.AddFilter("*.level", "关卡文件");
        _importDialog.FileSelected += OnImportFileSelected;
        _importDialog.Canceled += OnDialogCanceled;
        AddChild(_importDialog);
    }

    private void OnDialogCanceled()
    {
        // 空实现，让 Godot 自己处理关闭
    }

    private void OnBoardSizeChanged(double value)
    {
        int newRows = (int)_rowsSpinBox!.Value;
        int newCols = (int)_colsSpinBox!.Value;

        if (newRows != _boardRows || newCols != _boardCols)
        {
            var newPattern = new bool[newRows, newCols];
            for (int r = 0; r < Math.Min(newRows, _boardRows); r++)
            {
                for (int c = 0; c < Math.Min(newCols, _boardCols); c++)
                {
                    newPattern[r, c] = _targetPattern[r, c];
                }
            }
            _boardRows = newRows;
            _boardCols = newCols;
            _targetPattern = newPattern;
            RefreshBoardCanvas();
            UpdateStatus();
        }
    }

    private void OnClearBoard()
    {
        _targetPattern = new bool[_boardRows, _boardCols];
        RefreshBoardCanvas();
        UpdateStatus();
    }

    private void OnFillBoard()
    {
        for (int r = 0; r < _boardRows; r++)
            for (int c = 0; c < _boardCols; c++)
                _targetPattern[r, c] = true;
        RefreshBoardCanvas();
        UpdateStatus();
    }

    private void RefreshBoardCanvas()
    {
        _boardCanvas?.QueueRedraw();
    }

    private void OnBoardCanvasDraw()
    {
        if (_boardCanvas == null) return;

        var canvasSize = _boardCanvas.Size;
        int cellSize = (int)Math.Min(canvasSize.X / _boardCols, canvasSize.Y / _boardRows);
        cellSize = Math.Max(cellSize, 16);
        cellSize = Math.Min(cellSize, 40);

        float offsetX = (canvasSize.X - cellSize * _boardCols) / 2;
        float offsetY = (canvasSize.Y - cellSize * _boardRows) / 2;

        var emptyColor = new Color(0.25f, 0.25f, 0.28f, 1f);
        var filledColor = new Color(0.3f, 0.7f, 0.9f, 1f);
        var gridColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        var borderColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        // Draw cells
        for (int r = 0; r < _boardRows; r++)
        {
            for (int c = 0; c < _boardCols; c++)
            {
                var rect = new Rect2(offsetX + c * cellSize + 1, offsetY + r * cellSize + 1, cellSize - 2, cellSize - 2);
                var color = _targetPattern[r, c] ? filledColor : emptyColor;
                _boardCanvas.DrawRect(rect, color);
            }
        }

        // Draw grid
        for (int c = 0; c <= _boardCols; c++)
        {
            float x = offsetX + c * cellSize;
            float width = (c % 5 == 0) ? 2f : 1f;
            var color = (c % 5 == 0) ? borderColor : gridColor;
            _boardCanvas.DrawLine(new Vector2(x, offsetY), new Vector2(x, offsetY + _boardRows * cellSize), color, width);
        }
        for (int r = 0; r <= _boardRows; r++)
        {
            float y = offsetY + r * cellSize;
            float width = (r % 5 == 0) ? 2f : 1f;
            var color = (r % 5 == 0) ? borderColor : gridColor;
            _boardCanvas.DrawLine(new Vector2(offsetX, y), new Vector2(offsetX + _boardCols * cellSize, y), color, width);
        }

        // Border
        var boardRect = new Rect2(offsetX, offsetY, _boardCols * cellSize, _boardRows * cellSize);
        _boardCanvas.DrawRect(boardRect, borderColor, false, 3f);
    }

    private void OnBoardCanvasInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            var (row, col) = GetCellFromMousePosition(mouseEvent.Position);
            if (row >= 0 && row < _boardRows && col >= 0 && col < _boardCols)
            {
                _targetPattern[row, col] = !_targetPattern[row, col];
                RefreshBoardCanvas();
                UpdateStatus();
            }
        }
    }

    private (int row, int col) GetCellFromMousePosition(Vector2 pos)
    {
        if (_boardCanvas == null) return (-1, -1);

        var canvasSize = _boardCanvas.Size;
        int cellSize = (int)Math.Min(canvasSize.X / _boardCols, canvasSize.Y / _boardRows);
        cellSize = Math.Max(cellSize, 16);
        cellSize = Math.Min(cellSize, 40);

        float offsetX = (canvasSize.X - cellSize * _boardCols) / 2;
        float offsetY = (canvasSize.Y - cellSize * _boardRows) / 2;

        int col = (int)((pos.X - offsetX) / cellSize);
        int row = (int)((pos.Y - offsetY) / cellSize);

        return (row, col);
    }

    private void RefreshShapeList()
    {
        if (_shapeListContainer == null || _shapeRegistry == null) return;

        foreach (var child in _shapeListContainer.GetChildren())
            child.QueueFree();

        // 内置形状
        foreach (var shapeId in _shapeRegistry.ShapeIds)
        {
            var shape = _shapeRegistry.GetShape(shapeId);
            if (shape == null) continue;

            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 8);

            var preview = CreateShapePreview(shape, 16);
            hbox.AddChild(preview);

            var button = new Button
            {
                Text = $"{shapeId} ({shape.CellCount}格)",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            string id = shapeId;
            button.Pressed += () => AddShape(id);
            hbox.AddChild(button);

            _shapeListContainer.AddChild(hbox);
        }

        // 自定义形状
        if (_customShapes.Count > 0)
        {
            var separator = new HSeparator();
            _shapeListContainer.AddChild(separator);

            var customLabel = new Label { Text = "自定义形状:" };
            customLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.7f, 0.5f));
            _shapeListContainer.AddChild(customLabel);

            foreach (var (shapeId, shape) in _customShapes)
            {
                var hbox = new HBoxContainer();
                hbox.AddThemeConstantOverride("separation", 8);

                var preview = CreateShapePreview(shape, 16, new Color(0.9f, 0.6f, 0.3f));
                hbox.AddChild(preview);

                var button = new Button
                {
                    Text = $"{shapeId} ({shape.CellCount}格)",
                    SizeFlagsHorizontal = SizeFlags.ExpandFill
                };
                string id = shapeId;
                button.Pressed += () => AddShape(id);
                hbox.AddChild(button);

                var editButton = new Button { Text = "✎" };
                editButton.Pressed += () => OnEditCustomShape(id);
                hbox.AddChild(editButton);

                var deleteButton = new Button { Text = "×" };
                deleteButton.Pressed += () => OnDeleteCustomShape(id);
                hbox.AddChild(deleteButton);

                _shapeListContainer.AddChild(hbox);
            }
        }
    }

    private Control CreateShapePreview(ShapeData shape, int cellSize, Color? color = null)
    {
        var previewColor = color ?? new Color(0.3f, 0.7f, 0.9f, 1f);
        var canvas = new Control
        {
            CustomMinimumSize = new Vector2(shape.Cols * cellSize, shape.Rows * cellSize)
        };

        canvas.Draw += () =>
        {
            for (int r = 0; r < shape.Rows; r++)
            {
                for (int c = 0; c < shape.Cols; c++)
                {
                    if (shape.Matrix[r, c])
                    {
                        var rect = new Rect2(c * cellSize + 1, r * cellSize + 1, cellSize - 2, cellSize - 2);
                        canvas.DrawRect(rect, previewColor);
                    }
                }
            }
        };

        return canvas;
    }

    private void AddShape(string shapeId)
    {
        ShapeData? shape = _shapeRegistry?.GetShape(shapeId);
        if (shape == null)
        {
            _customShapes.TryGetValue(shapeId, out shape);
        }
        if (shape == null) return;

        _selectedShapes.Add(shape);
        _selectedShapeIds.Add(shapeId);
        RefreshSelectedShapes();
        UpdateStatus();
    }

    private void RemoveShapeAt(int index)
    {
        if (index < 0 || index >= _selectedShapes.Count) return;

        _selectedShapes.RemoveAt(index);
        _selectedShapeIds.RemoveAt(index);
        RefreshSelectedShapes();
        UpdateStatus();
    }

    private void RefreshSelectedShapes()
    {
        if (_selectedShapesContainer == null) return;

        foreach (var child in _selectedShapesContainer.GetChildren())
            child.QueueFree();

        for (int i = 0; i < _selectedShapes.Count; i++)
        {
            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 8);

            var preview = CreateShapePreview(_selectedShapes[i], 12);
            hbox.AddChild(preview);

            var label = new Label
            {
                Text = $"{_selectedShapeIds[i]}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            hbox.AddChild(label);

            int index = i;
            var removeBtn = new Button { Text = "×", CustomMinimumSize = new Vector2(30, 0) };
            removeBtn.Pressed += () => RemoveShapeAt(index);
            hbox.AddChild(removeBtn);

            _selectedShapesContainer.AddChild(hbox);
        }
    }

    private void UpdateStatus()
    {
        if (_statusLabel == null) return;

        int targetCells = 0;
        for (int r = 0; r < _boardRows; r++)
            for (int c = 0; c < _boardCols; c++)
                if (_targetPattern[r, c]) targetCells++;

        int shapeCells = _selectedShapes.Sum(s => s.CellCount);

        string status = $"目标格数: {targetCells}\n形状格数: {shapeCells}";
        if (targetCells != shapeCells)
        {
            status += "\n⚠ 格数不匹配!";
            _statusLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.6f, 0.3f));
        }
        else
        {
            _statusLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.9f, 0.6f));
        }

        _statusLabel.Text = status;
    }

    private void OnCheckUniqueness()
    {
        int targetCells = 0;
        for (int r = 0; r < _boardRows; r++)
            for (int c = 0; c < _boardCols; c++)
                if (_targetPattern[r, c]) targetCells++;

        int shapeCells = _selectedShapes.Sum(s => s.CellCount);

        if (targetCells != shapeCells)
        {
            ShowMessage("格数不匹配", $"目标格数 ({targetCells}) 与形状格数 ({shapeCells}) 不一致。");
            return;
        }

        if (_selectedShapes.Count == 0)
        {
            ShowMessage("无形状", "请先添加形状。");
            return;
        }

        var board = new BoardData(_boardRows, _boardCols, _targetPattern);
        var converter = new BoardToDLXConverter(board, _selectedShapes.ToArray());
        var matrix = converter.BuildMatrix();
        var placements = converter.GetPlacements();
        int duplicateFactor = converter.GetDuplicateFactor();

        if (matrix.GetLength(0) == 0)
        {
            ShowMessage("无解", "当前配置无法生成有效的放置方案。");
            return;
        }

        var solver = new ExactCoverSolver(matrix);
        var allSolutions = solver.SolveAll();
        int rawCount = allSolutions.Count;
        int uniqueCount = rawCount / duplicateFactor;

        if (uniqueCount == 0)
        {
            ShowMessage("无解", "当前配置没有解。");
        }
        else if (uniqueCount == 1)
        {
            var solutionPreview = CreateSolutionPreview(allSolutions[0], placements);
            ShowMessage("唯一解 ✓", "当前配置有且仅有一个解！", solutionPreview);
        }
        else
        {
            // 显示前两个不同的解
            var preview = CreateMultiSolutionPreview(allSolutions, placements, duplicateFactor);
            ShowMessage("多解", $"当前配置有 {uniqueCount} 个不同的解。建议调整形状或目标图案。", preview);
        }
    }

    private Control CreateSolutionPreview(List<int> solution, List<PlacementInfo> placements)
    {
        int cellSize = Math.Max(8, Math.Min(20, 200 / Math.Max(_boardRows, _boardCols)));
        var canvas = new Control
        {
            CustomMinimumSize = new Vector2(_boardCols * cellSize + 4, _boardRows * cellSize + 4)
        };

        var colors = GenerateShapeColors(_selectedShapes.Count);
        var solutionPlacements = solution.Select(idx => placements[idx]).ToList();

        canvas.Draw += () => DrawSolutionOnCanvas(canvas, solutionPlacements, colors, cellSize);
        return canvas;
    }

    private Control CreateMultiSolutionPreview(List<List<int>> allSolutions, List<PlacementInfo> placements, int duplicateFactor)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 20);

        var colors = GenerateShapeColors(_selectedShapes.Count);
        int cellSize = Math.Max(6, Math.Min(16, 150 / Math.Max(_boardRows, _boardCols)));

        // 找到两个不同的解（跳过重复的排列）
        var uniqueSolutions = new List<List<int>>();
        var seenPatterns = new HashSet<string>();

        foreach (var solution in allSolutions)
        {
            var pattern = GetSolutionPattern(solution, placements);
            if (!seenPatterns.Contains(pattern))
            {
                seenPatterns.Add(pattern);
                uniqueSolutions.Add(solution);
                if (uniqueSolutions.Count >= 2) break;
            }
        }

        for (int i = 0; i < Math.Min(2, uniqueSolutions.Count); i++)
        {
            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 4);

            var label = new Label { Text = $"解 {i + 1}" };
            label.AddThemeFontSizeOverride("font_size", 12);
            vbox.AddChild(label);

            var canvas = new Control
            {
                CustomMinimumSize = new Vector2(_boardCols * cellSize + 4, _boardRows * cellSize + 4)
            };

            var solutionPlacements = uniqueSolutions[i].Select(idx => placements[idx]).ToList();
            int idx = i;
            canvas.Draw += () => DrawSolutionOnCanvas(canvas, solutionPlacements, colors, cellSize);
            vbox.AddChild(canvas);

            hbox.AddChild(vbox);
        }

        return hbox;
    }

    private string GetSolutionPattern(List<int> solution, List<PlacementInfo> placements)
    {
        var grid = new int[_boardRows, _boardCols];
        foreach (var rowIdx in solution)
        {
            var p = placements[rowIdx];
            for (int sr = 0; sr < p.Shape.Rows; sr++)
            {
                for (int sc = 0; sc < p.Shape.Cols; sc++)
                {
                    if (p.Shape.Matrix[sr, sc])
                    {
                        grid[p.Row + sr, p.Col + sc] = p.ShapeIndex + 1;
                    }
                }
            }
        }

        // 生成规范化的模式字符串（按位置排序，不区分形状ID）
        var cells = new List<(int r, int c, int v)>();
        for (int r = 0; r < _boardRows; r++)
            for (int c = 0; c < _boardCols; c++)
                if (grid[r, c] > 0)
                    cells.Add((r, c, grid[r, c]));

        // 重新编号使相同形状的不同实例不可区分
        var mapping = new Dictionary<int, int>();
        int nextId = 1;
        foreach (var cell in cells.OrderBy(c => c.r).ThenBy(c => c.c))
        {
            if (!mapping.ContainsKey(cell.v))
                mapping[cell.v] = nextId++;
        }

        return string.Join(",", cells.OrderBy(c => c.r).ThenBy(c => c.c).Select(c => $"{c.r},{c.c},{mapping[c.v]}"));
    }

    private void DrawSolutionOnCanvas(Control canvas, List<PlacementInfo> solutionPlacements, Color[] colors, int cellSize)
    {
        var bgColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        var gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        // 背景
        canvas.DrawRect(new Rect2(0, 0, _boardCols * cellSize, _boardRows * cellSize), bgColor);

        // 绘制形状
        foreach (var p in solutionPlacements)
        {
            var color = colors[p.ShapeIndex % colors.Length];
            for (int sr = 0; sr < p.Shape.Rows; sr++)
            {
                for (int sc = 0; sc < p.Shape.Cols; sc++)
                {
                    if (p.Shape.Matrix[sr, sc])
                    {
                        var rect = new Rect2((p.Col + sc) * cellSize + 1, (p.Row + sr) * cellSize + 1, cellSize - 2, cellSize - 2);
                        canvas.DrawRect(rect, color);
                    }
                }
            }
        }

        // 网格线
        for (int c = 0; c <= _boardCols; c++)
            canvas.DrawLine(new Vector2(c * cellSize, 0), new Vector2(c * cellSize, _boardRows * cellSize), gridColor);
        for (int r = 0; r <= _boardRows; r++)
            canvas.DrawLine(new Vector2(0, r * cellSize), new Vector2(_boardCols * cellSize, r * cellSize), gridColor);
    }

    private static Color[] GenerateShapeColors(int count)
    {
        var baseColors = new[]
        {
            new Color(0.3f, 0.7f, 0.9f),   // 蓝
            new Color(0.9f, 0.5f, 0.3f),   // 橙
            new Color(0.5f, 0.9f, 0.5f),   // 绿
            new Color(0.9f, 0.4f, 0.6f),   // 粉
            new Color(0.7f, 0.5f, 0.9f),   // 紫
            new Color(0.9f, 0.9f, 0.4f),   // 黄
            new Color(0.4f, 0.9f, 0.9f),   // 青
            new Color(0.9f, 0.6f, 0.6f),   // 红
        };

        var colors = new Color[count];
        for (int i = 0; i < count; i++)
        {
            colors[i] = baseColors[i % baseColors.Length];
        }
        return colors;
    }

    private void OnAutoSelectShapes()
    {
        int targetCells = 0;
        for (int r = 0; r < _boardRows; r++)
            for (int c = 0; c < _boardCols; c++)
                if (_targetPattern[r, c]) targetCells++;

        if (targetCells == 0)
        {
            ShowMessage("无目标", "请先在棋盘上绘制目标图案。");
            return;
        }

        if (_shapeRegistry == null)
        {
            ShowMessage("错误", "形状库未加载。");
            return;
        }

        // 收集所有可用形状
        var allShapes = new List<ShapeData>();
        var allShapeIds = new List<string>();
        foreach (var id in _shapeRegistry.ShapeIds)
        {
            var shape = _shapeRegistry.GetShape(id);
            if (shape != null)
            {
                allShapes.Add(shape);
                allShapeIds.Add(id);
            }
        }

        var board = new BoardData(_boardRows, _boardCols, _targetPattern);

        // 找最大形状尺寸
        int maxShapeSize = allShapes.Max(s => s.CellCount);
        int minShapesNeeded = (targetCells + maxShapeSize - 1) / maxShapeSize;
        
        // 动态设置最大形状数（至少是最小需要数，最多20个）
        int maxShapeCount = Math.Max(8, Math.Min(20, minShapesNeeded + 2));
        
        // 如果目标太大，直接提示
        if (minShapesNeeded > maxShapeCount)
        {
            ShowMessage("目标过大", $"目标有 {targetCells} 格，最大形状 {maxShapeSize} 格。\n至少需要 {minShapesNeeded} 个形状，超出搜索限制 ({maxShapeCount})。\n\n建议减少目标格数或手动选择形状。");
            return;
        }

        var selector = new ShapeSelector(board, allShapes.ToArray(), 10000, maxShapeCount);

        // 显示搜索中提示
        UpdateStatus();
        _statusLabel!.Text += "\n🔍 搜索中...";

        // 搜索并获取详细结果
        var outcome = selector.FindUniqueSolutionWithDetails();

        if (outcome.Result == ShapeSelectResult.Found && outcome.ShapeIndices != null)
        {
            // 清空当前选择
            _selectedShapes.Clear();
            _selectedShapeIds.Clear();

            // 添加找到的形状
            foreach (var idx in outcome.ShapeIndices)
            {
                _selectedShapes.Add(allShapes[idx]);
                _selectedShapeIds.Add(allShapeIds[idx]);
            }

            RefreshSelectedShapes();
            UpdateStatus();
            ShowMessage("找到唯一解配置 ✓", 
                $"已自动选择 {outcome.ShapeIndices.Count} 个形状。\n" +
                $"搜索用时 {outcome.ElapsedMs}ms，检查 {outcome.SearchCount} 个组合。");
        }
        else
        {
            UpdateStatus();
            string title = outcome.Result switch
            {
                ShapeSelectResult.Timeout => "搜索超时",
                ShapeSelectResult.TargetTooLarge => "目标过大",
                ShapeSelectResult.NoShapes => "无可用形状",
                ShapeSelectResult.NoValidPlacements => "形状无法放置",
                _ => "未找到唯一解"
            };
            
            string detail = outcome.Result switch
            {
                ShapeSelectResult.Timeout => 
                    $"搜索用时 {outcome.ElapsedMs}ms，检查了 {outcome.SearchCount} 个组合。\n\n" +
                    "建议：\n• 减少目标格数\n• 手动选择形状",
                ShapeSelectResult.TargetTooLarge => 
                    outcome.Message + "\n\n建议减少目标格数或手动选择形状。",
                ShapeSelectResult.NoShapes => 
                    "没有可用的形状库。\n\n请确保 Shapes 目录中有形状文件。",
                ShapeSelectResult.NoValidPlacements => 
                    outcome.Message + "\n\n目标形状可能太不规则，没有形状能放入。",
                _ => 
                    $"检查了 {outcome.SearchCount} 个组合，剪枝 {outcome.PruneCount} 次。\n\n" +
                    "建议：\n• 调整目标图案\n• 使用更多不同形状"
            };
            
            ShowMessage(title, detail);
        }
    }

    private void OnExport()
    {
        _exportDialog?.PopupCentered();
    }

    private void OnExportFileSelected(string path)
    {
        _exportDialog?.Hide();

        try
        {
            var levelId = System.IO.Path.GetFileNameWithoutExtension(path);
            var levelName = _levelNameEdit?.Text ?? "新关卡";
            var difficulty = (int)(_difficultySpinBox?.Value ?? 1);

            var shapeInfos = new List<(string ShapeId, bool IsBuiltin, string BuiltinName, ShapeFileData? CustomShape, string? Color)>();
            for (int i = 0; i < _selectedShapeIds.Count; i++)
            {
                var originalId = _selectedShapeIds[i];
                var shapeId = $"shape_{i}";
                
                // 检查是否是自定义形状
                if (_customShapeFiles.TryGetValue(originalId, out var customShapeFile))
                {
                    shapeInfos.Add((shapeId, false, originalId, customShapeFile, null));
                }
                else
                {
                    shapeInfos.Add((shapeId, true, originalId, null, null));
                }
            }

            var levelData = new LevelData
            {
                Id = levelId,
                Name = levelName,
                Difficulty = difficulty,
                Rows = _boardRows,
                Cols = _boardCols,
                Target = _targetPattern,
                Shapes = _selectedShapes.ToArray()
            };

            var package = LevelPackage.FromLevelData(levelData, shapeInfos);
            package.SaveToFile(path);

            ShowMessage("导出成功", $"关卡已保存到:\n{path}");
        }
        catch (Exception e)
        {
            ShowMessage("导出失败", e.Message);
        }
    }

    private void OnImport()
    {
        _importDialog?.PopupCentered();
    }

    private void OnImportFileSelected(string path)
    {
        _importDialog?.Hide();

        try
        {
            // 直接加载 LevelPackage 以获取自定义形状信息
            var package = LevelPackage.LoadFromFile(path);
            var level = package.ToLevelData(GodotLevelLoader.GetShapeRegistry().CreateResolver());

            _boardRows = level.Rows;
            _boardCols = level.Cols;
            _rowsSpinBox!.Value = _boardRows;
            _colsSpinBox!.Value = _boardCols;

            if (level.Target != null)
            {
                _targetPattern = (bool[,])level.Target.Clone();
            }
            else
            {
                _targetPattern = new bool[_boardRows, _boardCols];
                for (int r = 0; r < _boardRows; r++)
                    for (int c = 0; c < _boardCols; c++)
                        _targetPattern[r, c] = true;
            }

            _levelNameEdit!.Text = level.Name;
            _difficultySpinBox!.Value = level.Difficulty;

            // 加载自定义形状
            foreach (var (filename, shapeFile) in package.CustomShapes)
            {
                var customId = shapeFile.Id;
                if (!_customShapes.ContainsKey(customId))
                {
                    _customShapes[customId] = shapeFile.ToShapeData();
                    _customShapeFiles[customId] = shapeFile;
                }
            }

            _selectedShapes.Clear();
            _selectedShapeIds.Clear();

            // 根据 metadata 还原形状引用
            for (int i = 0; i < package.Level.ShapeIds.Length; i++)
            {
                var shapeId = package.Level.ShapeIds[i];
                if (package.Metadata.ShapeIndex.TryGetValue(shapeId, out var source))
                {
                    var (isBuiltin, name) = LevelMetadata.ParseShapeSource(source);
                    if (isBuiltin)
                    {
                        var shape = _shapeRegistry?.GetShape(name);
                        if (shape != null)
                        {
                            _selectedShapes.Add(shape);
                            _selectedShapeIds.Add(name);
                        }
                    }
                    else
                    {
                        // 自定义形状
                        if (package.CustomShapes.TryGetValue(name, out var customFile))
                        {
                            var customId = customFile.Id;
                            _selectedShapes.Add(_customShapes[customId]);
                            _selectedShapeIds.Add(customId);
                        }
                    }
                }
            }

            RefreshBoardCanvas();
            RefreshShapeList();
            RefreshSelectedShapes();
            UpdateStatus();

            ShowMessage("导入成功", $"已加载关卡: {level.Name}");
        }
        catch (Exception e)
        {
            ShowMessage("导入失败", e.Message);
        }
    }

    private string? FindMatchingShapeId(ShapeData shape)
    {
        if (_shapeRegistry == null) return null;

        foreach (var id in _shapeRegistry.ShapeIds)
        {
            var builtinShape = _shapeRegistry.GetShape(id);
            if (builtinShape != null && ShapesMatch(shape, builtinShape))
            {
                return id;
            }
        }
        return null;
    }

    private static bool ShapesMatch(ShapeData a, ShapeData b)
    {
        if (a.Rows != b.Rows || a.Cols != b.Cols) return false;
        for (int r = 0; r < a.Rows; r++)
            for (int c = 0; c < a.Cols; c++)
                if (a.Matrix[r, c] != b.Matrix[r, c]) return false;
        return true;
    }

    private void OnBack()
    {
        HideAllDialogs();
        GameSession.Instance.GoToTitle();
    }

    private void HideAllDialogs()
    {
        _exportDialog?.Hide();
        _importDialog?.Hide();
        if (_currentMessageDialog != null && IsInstanceValid(_currentMessageDialog))
        {
            _currentMessageDialog.Hide();
            _currentMessageDialog.QueueFree();
            _currentMessageDialog = null;
        }
    }

    private void ShowMessage(string title, string message, Control? customContent = null)
    {
        // 清理之前的对话框（如果存在且已隐藏）
        if (_currentMessageDialog != null && IsInstanceValid(_currentMessageDialog))
        {
            if (!_currentMessageDialog.Visible)
            {
                _currentMessageDialog.QueueFree();
            }
            _currentMessageDialog = null;
        }

        _currentMessageDialog = new AcceptDialog
        {
            Title = title,
            Transient = false
        };

        if (customContent != null)
        {
            // 使用 VBoxContainer 垂直排列文本和自定义内容
            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 12);

            var label = new Label { Text = message };
            vbox.AddChild(label);
            vbox.AddChild(customContent);

            _currentMessageDialog.AddChild(vbox);
        }
        else
        {
            _currentMessageDialog.DialogText = message;
        }

        AddChild(_currentMessageDialog);
        _currentMessageDialog.PopupCentered();
    }

    private void CleanupMessageDialog()
    {
        if (_currentMessageDialog != null && IsInstanceValid(_currentMessageDialog))
        {
            _currentMessageDialog.Hide();
            _currentMessageDialog.QueueFree();
            _currentMessageDialog = null;
        }
    }

    #region Custom Shape Editor

    private const int ShapeEditorMaxSize = 10;
    private bool[,]? _editingShapeMatrix;
    private string? _editingShapeId;
    private LineEdit? _shapeNameEdit;
    private SpinBox? _shapeRowsSpinBox;
    private SpinBox? _shapeColsSpinBox;
    private Control? _shapeEditorCanvas;

    private void OnNewCustomShape()
    {
        _editingShapeId = null;
        _editingShapeMatrix = new bool[4, 4];
        _editingShapeMatrix[1, 1] = true;
        ShowShapeEditorDialog("新建自定义形状", $"Custom{_customShapeCounter}");
    }

    private void OnEditCustomShape(string shapeId)
    {
        if (!_customShapes.TryGetValue(shapeId, out var shape)) return;

        _editingShapeId = shapeId;
        _editingShapeMatrix = new bool[shape.Rows, shape.Cols];
        for (int r = 0; r < shape.Rows; r++)
            for (int c = 0; c < shape.Cols; c++)
                _editingShapeMatrix[r, c] = shape.Matrix[r, c];

        var name = _customShapeFiles.TryGetValue(shapeId, out var fileData) ? fileData.Name : shapeId;
        ShowShapeEditorDialog("编辑自定义形状", name);
    }

    private void OnDeleteCustomShape(string shapeId)
    {
        // 从已选形状中移除
        for (int i = _selectedShapeIds.Count - 1; i >= 0; i--)
        {
            if (_selectedShapeIds[i] == shapeId)
            {
                _selectedShapeIds.RemoveAt(i);
                _selectedShapes.RemoveAt(i);
            }
        }

        _customShapes.Remove(shapeId);
        _customShapeFiles.Remove(shapeId);
        RefreshShapeList();
        RefreshSelectedShapes();
        UpdateStatus();
    }

    private void ShowShapeEditorDialog(string title, string defaultName)
    {
        if (_shapeEditorDialog != null && IsInstanceValid(_shapeEditorDialog))
        {
            _shapeEditorDialog.QueueFree();
        }

        _shapeEditorDialog = new Window
        {
            Title = title,
            Size = new Vector2I(400, 450),
            Transient = false,
            Exclusive = true
        };

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 15);
        margin.AddThemeConstantOverride("margin_right", 15);
        margin.AddThemeConstantOverride("margin_top", 15);
        margin.AddThemeConstantOverride("margin_bottom", 15);
        _shapeEditorDialog.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(vbox);

        // 名称
        var nameHBox = new HBoxContainer();
        nameHBox.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(nameHBox);

        nameHBox.AddChild(new Label { Text = "名称:" });
        _shapeNameEdit = new LineEdit { Text = defaultName, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        nameHBox.AddChild(_shapeNameEdit);

        // 尺寸
        var sizeHBox = new HBoxContainer();
        sizeHBox.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(sizeHBox);

        sizeHBox.AddChild(new Label { Text = "行:" });
        _shapeRowsSpinBox = new SpinBox { MinValue = 1, MaxValue = ShapeEditorMaxSize, Value = _editingShapeMatrix?.GetLength(0) ?? 4 };
        _shapeRowsSpinBox.ValueChanged += OnShapeEditorSizeChanged;
        sizeHBox.AddChild(_shapeRowsSpinBox);

        sizeHBox.AddChild(new Label { Text = "列:" });
        _shapeColsSpinBox = new SpinBox { MinValue = 1, MaxValue = ShapeEditorMaxSize, Value = _editingShapeMatrix?.GetLength(1) ?? 4 };
        _shapeColsSpinBox.ValueChanged += OnShapeEditorSizeChanged;
        sizeHBox.AddChild(_shapeColsSpinBox);

        // 画布
        var canvasLabel = new Label { Text = "点击格子切换 (至少1格):" };
        vbox.AddChild(canvasLabel);

        _shapeEditorCanvas = new Control
        {
            CustomMinimumSize = new Vector2(300, 200),
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _shapeEditorCanvas.GuiInput += OnShapeEditorCanvasInput;
        _shapeEditorCanvas.Draw += OnShapeEditorCanvasDraw;
        vbox.AddChild(_shapeEditorCanvas);

        // 按钮
        var buttonHBox = new HBoxContainer();
        buttonHBox.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(buttonHBox);

        var cancelButton = new Button { Text = "取消", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        cancelButton.Pressed += () => _shapeEditorDialog?.Hide();
        buttonHBox.AddChild(cancelButton);

        var saveButton = new Button { Text = "保存", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        saveButton.Pressed += OnSaveCustomShape;
        buttonHBox.AddChild(saveButton);

        AddChild(_shapeEditorDialog);
        _shapeEditorDialog.PopupCentered();
    }

    private void OnShapeEditorSizeChanged(double value)
    {
        if (_editingShapeMatrix == null || _shapeRowsSpinBox == null || _shapeColsSpinBox == null) return;

        int newRows = (int)_shapeRowsSpinBox.Value;
        int newCols = (int)_shapeColsSpinBox.Value;
        int oldRows = _editingShapeMatrix.GetLength(0);
        int oldCols = _editingShapeMatrix.GetLength(1);

        if (newRows == oldRows && newCols == oldCols) return;

        var newMatrix = new bool[newRows, newCols];
        for (int r = 0; r < Math.Min(newRows, oldRows); r++)
            for (int c = 0; c < Math.Min(newCols, oldCols); c++)
                newMatrix[r, c] = _editingShapeMatrix[r, c];

        _editingShapeMatrix = newMatrix;
        _shapeEditorCanvas?.QueueRedraw();
    }

    private void OnShapeEditorCanvasInput(InputEvent @event)
    {
        if (_editingShapeMatrix == null || _shapeEditorCanvas == null) return;

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            var (row, col) = GetShapeEditorCellFromMouse(mouseEvent.Position);
            if (row >= 0 && row < _editingShapeMatrix.GetLength(0) &&
                col >= 0 && col < _editingShapeMatrix.GetLength(1))
            {
                _editingShapeMatrix[row, col] = !_editingShapeMatrix[row, col];
                _shapeEditorCanvas.QueueRedraw();
            }
        }
    }

    private (int row, int col) GetShapeEditorCellFromMouse(Vector2 pos)
    {
        if (_editingShapeMatrix == null || _shapeEditorCanvas == null) return (-1, -1);

        int rows = _editingShapeMatrix.GetLength(0);
        int cols = _editingShapeMatrix.GetLength(1);
        var canvasSize = _shapeEditorCanvas.Size;

        int cellSize = (int)Math.Min(canvasSize.X / cols, canvasSize.Y / rows);
        cellSize = Math.Clamp(cellSize, 16, 40);

        float offsetX = (canvasSize.X - cellSize * cols) / 2;
        float offsetY = (canvasSize.Y - cellSize * rows) / 2;

        int col = (int)((pos.X - offsetX) / cellSize);
        int row = (int)((pos.Y - offsetY) / cellSize);

        return (row, col);
    }

    private void OnShapeEditorCanvasDraw()
    {
        if (_editingShapeMatrix == null || _shapeEditorCanvas == null) return;

        int rows = _editingShapeMatrix.GetLength(0);
        int cols = _editingShapeMatrix.GetLength(1);
        var canvasSize = _shapeEditorCanvas.Size;

        int cellSize = (int)Math.Min(canvasSize.X / cols, canvasSize.Y / rows);
        cellSize = Math.Clamp(cellSize, 16, 40);

        float offsetX = (canvasSize.X - cellSize * cols) / 2;
        float offsetY = (canvasSize.Y - cellSize * rows) / 2;

        var filledColor = new Color(0.9f, 0.6f, 0.3f);
        var emptyColor = new Color(0.2f, 0.2f, 0.25f);
        var borderColor = new Color(0.4f, 0.4f, 0.5f);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var rect = new Rect2(offsetX + c * cellSize, offsetY + r * cellSize, cellSize, cellSize);
                var innerRect = new Rect2(rect.Position.X + 1, rect.Position.Y + 1, cellSize - 2, cellSize - 2);

                _shapeEditorCanvas.DrawRect(rect, borderColor, false, 1);
                _shapeEditorCanvas.DrawRect(innerRect, _editingShapeMatrix[r, c] ? filledColor : emptyColor);
            }
        }
    }

    private void OnSaveCustomShape()
    {
        if (_editingShapeMatrix == null || _shapeNameEdit == null) return;

        // 检查至少有一个格子
        int cellCount = 0;
        for (int r = 0; r < _editingShapeMatrix.GetLength(0); r++)
            for (int c = 0; c < _editingShapeMatrix.GetLength(1); c++)
                if (_editingShapeMatrix[r, c]) cellCount++;

        if (cellCount == 0)
        {
            ShowMessage("错误", "形状至少需要1个格子。");
            return;
        }

        // 裁剪空白边缘
        var trimmed = TrimShapeMatrix(_editingShapeMatrix);
        var shape = new ShapeData(trimmed);

        string name = _shapeNameEdit.Text.Trim();
        if (string.IsNullOrEmpty(name)) name = $"Custom{_customShapeCounter}";

        string shapeId;
        if (_editingShapeId != null)
        {
            // 编辑现有形状
            shapeId = _editingShapeId;
            
            // 更新已选形状中的引用
            for (int i = 0; i < _selectedShapeIds.Count; i++)
            {
                if (_selectedShapeIds[i] == shapeId)
                {
                    _selectedShapes[i] = shape;
                }
            }
        }
        else
        {
            // 新建形状
            shapeId = $"Custom{_customShapeCounter}";
            while (_customShapes.ContainsKey(shapeId) || _shapeRegistry?.GetShape(shapeId) != null)
            {
                _customShapeCounter++;
                shapeId = $"Custom{_customShapeCounter}";
            }
            _customShapeCounter++;
        }

        _customShapes[shapeId] = shape;
        _customShapeFiles[shapeId] = ShapeFileData.FromShapeData(shape, shapeId, name);

        _shapeEditorDialog?.Hide();
        RefreshShapeList();
        RefreshSelectedShapes();
        UpdateStatus();
    }

    private static bool[,] TrimShapeMatrix(bool[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        int minRow = rows, maxRow = -1, minCol = cols, maxCol = -1;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (matrix[r, c])
                {
                    minRow = Math.Min(minRow, r);
                    maxRow = Math.Max(maxRow, r);
                    minCol = Math.Min(minCol, c);
                    maxCol = Math.Max(maxCol, c);
                }
            }
        }

        if (maxRow < 0) return new bool[1, 1];

        int newRows = maxRow - minRow + 1;
        int newCols = maxCol - minCol + 1;
        var trimmed = new bool[newRows, newCols];

        for (int r = 0; r < newRows; r++)
            for (int c = 0; c < newCols; c++)
                trimmed[r, c] = matrix[minRow + r, minCol + c];

        return trimmed;
    }

    #endregion
}
