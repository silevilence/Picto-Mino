namespace PictoMino.Core;

/// <summary>
/// 表示一个形状的放置方式。
/// </summary>
public readonly record struct PlacementInfo(int ShapeIndex, int Row, int Col);

/// <summary>
/// 将棋盘状态和形状集合转换为 DLX 精确覆盖矩阵。
/// 
/// 矩阵列 = [目标格子列...] + [形状使用列...]
/// 矩阵行 = 每种可能的放置方式
/// </summary>
public class BoardToDLXConverter
{
    private readonly BoardData _board;
    private readonly ShapeData[] _shapes;
    private readonly List<PlacementInfo> _placements;
    private readonly List<(int row, int col)> _targetCells;
    private readonly Dictionary<(int row, int col), int> _cellToColumnIndex;

    public BoardToDLXConverter(BoardData board, ShapeData[] shapes)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(shapes);

        _board = board;
        _shapes = shapes;
        _placements = new List<PlacementInfo>();
        _targetCells = new List<(int, int)>();
        _cellToColumnIndex = new Dictionary<(int, int), int>();
    }

    /// <summary>
    /// 构建精确覆盖矩阵。
    /// </summary>
    /// <returns>0-1 矩阵 [放置方式, 列]</returns>
    public int[,] BuildMatrix()
    {
        _placements.Clear();
        _targetCells.Clear();
        _cellToColumnIndex.Clear();

        // 收集目标格子
        CollectTargetCells();

        // 生成所有可能的放置方式
        GenerateAllPlacements();

        // 构建矩阵
        int numCols = _targetCells.Count + _shapes.Length;
        int numRows = _placements.Count;

        if (numRows == 0)
        {
            return new int[0, numCols > 0 ? numCols : 1];
        }

        var matrix = new int[numRows, numCols];

        for (int rowIdx = 0; rowIdx < _placements.Count; rowIdx++)
        {
            var placement = _placements[rowIdx];
            var shape = _shapes[placement.ShapeIndex];

            // 标记覆盖的格子
            for (int sr = 0; sr < shape.Rows; sr++)
            {
                for (int sc = 0; sc < shape.Cols; sc++)
                {
                    if (!shape.Matrix[sr, sc]) continue;

                    int boardRow = placement.Row + sr;
                    int boardCol = placement.Col + sc;

                    if (_cellToColumnIndex.TryGetValue((boardRow, boardCol), out int colIdx))
                    {
                        matrix[rowIdx, colIdx] = 1;
                    }
                }
            }

            // 标记使用的形状
            int shapeCol = _targetCells.Count + placement.ShapeIndex;
            matrix[rowIdx, shapeCol] = 1;
        }

        return matrix;
    }

    /// <summary>
    /// 获取所有可能的放置方式。
    /// 必须在 BuildMatrix() 之后调用。
    /// </summary>
    public List<PlacementInfo> GetPlacements()
    {
        return new List<PlacementInfo>(_placements);
    }

    /// <summary>
    /// 收集所有目标格子。
    /// </summary>
    private void CollectTargetCells()
    {
        int colIndex = 0;
        for (int r = 0; r < _board.Rows; r++)
        {
            for (int c = 0; c < _board.Cols; c++)
            {
                bool isTarget = _board.Target?[r, c] ?? true;
                if (isTarget)
                {
                    _targetCells.Add((r, c));
                    _cellToColumnIndex[(r, c)] = colIndex++;
                }
            }
        }
    }

    /// <summary>
    /// 生成所有可能的放置方式。
    /// </summary>
    private void GenerateAllPlacements()
    {
        for (int shapeIdx = 0; shapeIdx < _shapes.Length; shapeIdx++)
        {
            var shape = _shapes[shapeIdx];

            for (int r = 0; r <= _board.Rows - shape.Rows; r++)
            {
                for (int c = 0; c <= _board.Cols - shape.Cols; c++)
                {
                    if (IsValidPlacement(shape, r, c))
                    {
                        _placements.Add(new PlacementInfo(shapeIdx, r, c));
                    }
                }
            }
        }
    }

    /// <summary>
    /// 检查放置是否有效（所有占据格子都是目标格子）。
    /// </summary>
    private bool IsValidPlacement(ShapeData shape, int row, int col)
    {
        for (int sr = 0; sr < shape.Rows; sr++)
        {
            for (int sc = 0; sc < shape.Cols; sc++)
            {
                if (!shape.Matrix[sr, sc]) continue;

                int boardRow = row + sr;
                int boardCol = col + sc;

                // 检查是否在范围内
                if (!_board.IsInBounds(boardRow, boardCol))
                    return false;

                // 检查是否是目标格子
                bool isTarget = _board.Target?[boardRow, boardCol] ?? true;
                if (!isTarget)
                    return false;
            }
        }
        return true;
    }
}
