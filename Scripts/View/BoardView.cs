using Godot;
using PictoMino.Core;

namespace PictoMino.View;

/// <summary>
/// 棋盘视图层。订阅 BoardData 的变化事件，更新 TileMapLayer 显示。
/// </summary>
public partial class BoardView : Node2D
{
    /// <summary>TileSet 中的 Source ID</summary>
    private const int TileSourceId = 0;

    /// <summary>空格对应的 Atlas 坐标</summary>
    private static readonly Vector2I EmptyAtlasCoord = new(0, 0);

    /// <summary>填充格对应的 Atlas 坐标</summary>
    private static readonly Vector2I FilledAtlasCoord = new(1, 0);

    private TileMapLayer? _gridLayer;

    /// <summary>单元格大小（像素）</summary>
    [Export] public int CellSize { get; set; } = 32;

    private BoardData? _boardData;

    /// <summary>
    /// 绑定的棋盘数据。设置后会自动订阅事件并刷新视图。
    /// </summary>
    public BoardData? BoardData
    {
        get => _boardData;
        set
        {
            if (_boardData != null)
            {
                _boardData.OnCellChanged -= OnCellChanged;
            }

            _boardData = value;

            if (_boardData != null)
            {
                _boardData.OnCellChanged += OnCellChanged;
                RefreshAll();
            }
        }
    }

    public override void _Ready()
    {
        _gridLayer = GetNodeOrNull<TileMapLayer>("GridLayer");
        if (_gridLayer == null)
        {
            GD.PrintErr("BoardView: TileMapLayer not found.");
        }
    }

    public override void _ExitTree()
    {
        // 清理事件订阅
        if (_boardData != null)
        {
            _boardData.OnCellChanged -= OnCellChanged;
        }
    }

    /// <summary>
    /// 将棋盘坐标转换为世界坐标（格子中心）。
    /// </summary>
    public Vector2 GridToWorld(int row, int col)
    {
        return GlobalPosition + new Vector2(col * CellSize + CellSize / 2f, row * CellSize + CellSize / 2f);
    }

    /// <summary>
    /// 将世界坐标转换为棋盘坐标。
    /// </summary>
    public Vector2I WorldToGrid(Vector2 worldPos)
    {
        Vector2 localPos = worldPos - GlobalPosition;
        int col = Mathf.FloorToInt(localPos.X / CellSize);
        int row = Mathf.FloorToInt(localPos.Y / CellSize);
        return new Vector2I(col, row); // 返回 (col, row) 以便于输入处理
    }

    /// <summary>
    /// 将棋盘坐标转换为本地坐标（TileMapLayer 坐标系）。
    /// </summary>
    public Vector2I GridToTileCoord(int row, int col)
    {
        // TileMapLayer 使用 (x, y) 即 (col, row)
        return new Vector2I(col, row);
    }

    /// <summary>
    /// 刷新所有格子的显示。
    /// </summary>
    public void RefreshAll()
    {
        if (_gridLayer == null || _boardData == null) return;

        _gridLayer.Clear();

        for (int r = 0; r < _boardData.Rows; r++)
        {
            for (int c = 0; c < _boardData.Cols; c++)
            {
                UpdateTile(r, c, _boardData.GetCell(r, c));
            }
        }
    }

    /// <summary>
    /// 当单元格状态变化时的回调。
    /// </summary>
    private void OnCellChanged(int row, int col, int newValue)
    {
        UpdateTile(row, col, newValue);
    }

    /// <summary>
    /// 更新单个格子的 Tile 显示。
    /// </summary>
    private void UpdateTile(int row, int col, int value)
    {
        if (_gridLayer == null) return;

        Vector2I tileCoord = GridToTileCoord(row, col);
        Vector2I atlasCoord = value == 0 ? EmptyAtlasCoord : FilledAtlasCoord;

        _gridLayer.SetCell(tileCoord, TileSourceId, atlasCoord);
    }
}
