using System.Linq;

namespace PictoMino.Core;

/// <summary>
/// 棋盘网格状态。
/// 0 = 空格，正整数 = 被对应 ID 的方块占据。
/// </summary>
public class BoardData
{
    private readonly int[,] _cells;

    /// <summary>棋盘行数</summary>
    public int Rows { get; }

    /// <summary>棋盘列数</summary>
    public int Cols { get; }

    /// <summary>
    /// 目标图案。true 表示该格应被填充，false 表示应为空。
    /// 为 null 时，胜利条件为全部填满。
    /// </summary>
    public bool[,]? Target { get; }

    /// <summary>
    /// 当任意格子状态变化时触发。参数为 (row, col, newValue)。
    /// </summary>
    public event Action<int, int, int>? OnCellChanged;

    public BoardData(int rows, int cols, bool[,]? target = null)
    {
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (cols <= 0) throw new ArgumentOutOfRangeException(nameof(cols));

        Rows = rows;
        Cols = cols;
        _cells = new int[rows, cols];

        if (target != null)
        {
            if (target.GetLength(0) != rows || target.GetLength(1) != cols)
                throw new ArgumentException("Target dimensions must match board dimensions.");
            Target = (bool[,])target.Clone();
        }
    }

    /// <summary>获取格子值。0 = 空。</summary>
    public int GetCell(int row, int col)
    {
        ValidateCoords(row, col);
        return _cells[row, col];
    }

    /// <summary>设置格子值并触发事件。</summary>
    public void SetCell(int row, int col, int value)
    {
        ValidateCoords(row, col);
        if (_cells[row, col] == value) return;
        _cells[row, col] = value;
        OnCellChanged?.Invoke(row, col, value);
    }

    /// <summary>检查坐标是否在棋盘范围内。</summary>
    public bool IsInBounds(int row, int col)
        => row >= 0 && row < Rows && col >= 0 && col < Cols;

    /// <summary>
    /// 放置状态枚举。
    /// </summary>
    public enum PlacementStatus
    {
        /// <summary>可以放置</summary>
        Valid,
        /// <summary>部分超出边界</summary>
        OutOfBounds,
        /// <summary>与其他形状重叠</summary>
        Overlapping
    }

    /// <summary>
    /// 检查形状在指定位置的放置状态。
    /// </summary>
    /// <param name="shape">要放置的形状</param>
    /// <param name="row">形状左上角对应的棋盘行坐标</param>
    /// <param name="col">形状左上角对应的棋盘列坐标</param>
    /// <returns>放置状态</returns>
    public PlacementStatus CheckPlacement(ShapeData shape, int row, int col)
    {
        ArgumentNullException.ThrowIfNull(shape);

        bool hasOverlap = false;

        for (int r = 0; r < shape.Rows; r++)
        {
            for (int c = 0; c < shape.Cols; c++)
            {
                if (!shape.Matrix[r, c]) continue;

                int boardRow = row + r;
                int boardCol = col + c;

                if (!IsInBounds(boardRow, boardCol)) return PlacementStatus.OutOfBounds;
                if (_cells[boardRow, boardCol] != 0) hasOverlap = true;
            }
        }

        return hasOverlap ? PlacementStatus.Overlapping : PlacementStatus.Valid;
    }

    /// <summary>
    /// 尝试将形状放置到棋盘上。
    /// 形状的每个填充格必须在范围内且对应棋盘格为空（0），才能放置成功。
    /// </summary>
    /// <param name="shape">要放置的形状</param>
    /// <param name="row">形状左上角对应的棋盘行坐标</param>
    /// <param name="col">形状左上角对应的棋盘列坐标</param>
    /// <param name="shapeId">形状 ID（必须 &gt; 0）</param>
    /// <returns>放置成功返回 true，否则返回 false（不修改棋盘）。</returns>
    public bool TryPlace(ShapeData shape, int row, int col, int shapeId)
    {
        ArgumentNullException.ThrowIfNull(shape);
        if (shapeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(shapeId), "Shape ID must be positive.");

        var status = CheckPlacement(shape, row, col);
        if (status != PlacementStatus.Valid) return false;

        // 放置所有格子
        PlaceInternal(shape, row, col, shapeId);
        return true;
    }

    /// <summary>
    /// 强制将形状放置到棋盘上，即使有重叠也会放置。
    /// 被重叠的形状会被完全移除（不仅仅是重叠部分）。
    /// </summary>
    /// <param name="shape">要放置的形状</param>
    /// <param name="row">形状左上角对应的棋盘行坐标</param>
    /// <param name="col">形状左上角对应的棋盘列坐标</param>
    /// <param name="shapeId">形状 ID（必须 &gt; 0）</param>
    /// <returns>被覆盖的形状 ID 集合（不包含 0）。如果超出边界则返回 null。</returns>
    public HashSet<int>? ForcePlace(ShapeData shape, int row, int col, int shapeId)
    {
        ArgumentNullException.ThrowIfNull(shape);
        if (shapeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(shapeId), "Shape ID must be positive.");

        var status = CheckPlacement(shape, row, col);
        if (status == PlacementStatus.OutOfBounds) return null;

        // 收集被重叠的形状 ID
        var overwrittenIds = new HashSet<int>();
        for (int r = 0; r < shape.Rows; r++)
        {
            for (int c = 0; c < shape.Cols; c++)
            {
                if (!shape.Matrix[r, c]) continue;

                int boardRow = row + r;
                int boardCol = col + c;
                int existingId = _cells[boardRow, boardCol];
                if (existingId != 0)
                {
                    overwrittenIds.Add(existingId);
                }
            }
        }

        // 先完全移除被覆盖的形状（整个形状，不仅仅是重叠部分）
        foreach (int overwrittenId in overwrittenIds)
        {
            Remove(overwrittenId);
        }

        // 放置新形状
        PlaceInternal(shape, row, col, shapeId);
        return overwrittenIds;
    }

    /// <summary>
    /// 内部放置方法。
    /// </summary>
    private void PlaceInternal(ShapeData shape, int row, int col, int shapeId)
    {
        for (int r = 0; r < shape.Rows; r++)
        {
            for (int c = 0; c < shape.Cols; c++)
            {
                if (!shape.Matrix[r, c]) continue;
                SetCell(row + r, col + c, shapeId);
            }
        }
    }

    /// <summary>
    /// 移除指定 ID 的所有格子（设为 0）。
    /// </summary>
    /// <param name="shapeId">要移除的形状 ID（必须 &gt; 0）</param>
    /// <returns>实际移除的格子数。</returns>
    public int Remove(int shapeId)
    {
        if (shapeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(shapeId), "Shape ID must be positive.");

        int count = 0;
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                if (_cells[r, c] == shapeId)
                {
                    SetCell(r, c, 0);
                    count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// 检查胜利条件。
    /// 若有 Target：填充状态与 Target 完全匹配。
    /// 若无 Target：所有格子都非空。
    /// </summary>
    public bool CheckWinCondition()
    {
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                bool isFilled = _cells[r, c] != 0;
                bool shouldBeFilled = Target?[r, c] ?? true;
                if (isFilled != shouldBeFilled) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 获取指定行的提示数字（连续填充块的长度列表）。
    /// 基于 Target 计算，若无 Target 则视为全填充。
    /// </summary>
    public int[] GetRowHints(int row)
    {
        ValidateCoords(row, 0);
        return CalculateHints(Enumerable.Range(0, Cols).Select(c => Target?[row, c] ?? true));
    }

    /// <summary>
    /// 获取指定列的提示数字（连续填充块的长度列表）。
    /// 基于 Target 计算，若无 Target 则视为全填充。
    /// </summary>
    public int[] GetColHints(int col)
    {
        ValidateCoords(0, col);
        return CalculateHints(Enumerable.Range(0, Rows).Select(r => Target?[r, col] ?? true));
    }

    /// <summary>
    /// 获取所有行的提示数字。
    /// </summary>
    public int[][] GetAllRowHints()
    {
        var hints = new int[Rows][];
        for (int r = 0; r < Rows; r++)
        {
            hints[r] = GetRowHints(r);
        }
        return hints;
    }

    /// <summary>
    /// 获取所有列的提示数字。
    /// </summary>
    public int[][] GetAllColHints()
    {
        var hints = new int[Cols][];
        for (int c = 0; c < Cols; c++)
        {
            hints[c] = GetColHints(c);
        }
        return hints;
    }

    /// <summary>
    /// 获取指定行的当前填充提示数字（基于当前棋盘状态）。
    /// </summary>
    public int[] GetCurrentRowHints(int row)
    {
        ValidateCoords(row, 0);
        return CalculateHints(Enumerable.Range(0, Cols).Select(c => _cells[row, c] != 0));
    }

    /// <summary>
    /// 获取指定列的当前填充提示数字（基于当前棋盘状态）。
    /// </summary>
    public int[] GetCurrentColHints(int col)
    {
        ValidateCoords(0, col);
        return CalculateHints(Enumerable.Range(0, Rows).Select(r => _cells[r, col] != 0));
    }

    /// <summary>
    /// 获取指定行的填充格子数。
    /// </summary>
    public int GetRowFilledCount(int row)
    {
        ValidateCoords(row, 0);
        int count = 0;
        for (int c = 0; c < Cols; c++)
            if (_cells[row, c] != 0) count++;
        return count;
    }

    /// <summary>
    /// 获取指定列的填充格子数。
    /// </summary>
    public int GetColFilledCount(int col)
    {
        ValidateCoords(0, col);
        int count = 0;
        for (int r = 0; r < Rows; r++)
            if (_cells[r, col] != 0) count++;
        return count;
    }

    /// <summary>
    /// 计算一行/列的提示数字。
    /// </summary>
    private static int[] CalculateHints(IEnumerable<bool> cells)
    {
        var hints = new List<int>();
        int currentRun = 0;

        foreach (bool filled in cells)
        {
            if (filled)
            {
                currentRun++;
            }
            else if (currentRun > 0)
            {
                hints.Add(currentRun);
                currentRun = 0;
            }
        }

        if (currentRun > 0)
        {
            hints.Add(currentRun);
        }

        // 若整行/列都是空的，返回 [0]
        return hints.Count > 0 ? hints.ToArray() : new[] { 0 };
    }

    private void ValidateCoords(int row, int col)
    {
        if (!IsInBounds(row, col))
            throw new ArgumentOutOfRangeException($"({row},{col}) is out of bounds ({Rows}x{Cols}).");
    }
}
