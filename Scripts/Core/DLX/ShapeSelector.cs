namespace PictoMino.Core;

/// <summary>
/// 自动选择形状以填满目标图案并保证唯一解。
/// 使用回溯搜索 + DLX验证。
/// </summary>
public class ShapeSelector
{
    private readonly BoardData _board;
    private readonly ShapeData[] _availableShapes;
    private readonly int _targetCellCount;
    private readonly int _maxSearchTime;
    private readonly int _maxShapeCount;
    
    private List<int>? _bestSolution;
    private int _searchCount;
    private int _pruneCount;
    private DateTime _startTime;

    /// <summary>
    /// 创建形状选择器。
    /// </summary>
    /// <param name="board">目标棋盘</param>
    /// <param name="availableShapes">可用形状列表</param>
    /// <param name="maxSearchTimeMs">最大搜索时间（毫秒）</param>
    /// <param name="maxShapeCount">最大形状数量</param>
    public ShapeSelector(BoardData board, ShapeData[] availableShapes, int maxSearchTimeMs = 5000, int maxShapeCount = 6)
    {
        _board = board;
        _availableShapes = availableShapes;
        _maxSearchTime = maxSearchTimeMs;
        _maxShapeCount = maxShapeCount;
        
        // 计算目标格数
        _targetCellCount = 0;
        for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Cols; c++)
                if (board.Target?[r, c] ?? true)
                    _targetCellCount++;
    }

    /// <summary>
    /// 搜索能产生唯一解的形状组合。
    /// </summary>
    /// <returns>形状索引列表（可重复），null表示未找到</returns>
    public List<int>? FindUniqueSolution()
    {
        _bestSolution = null;
        _searchCount = 0;
        _pruneCount = 0;
        _startTime = DateTime.Now;

        // 过滤掉太大的形状
        var validIndices = Enumerable.Range(0, _availableShapes.Length)
            .Where(i => _availableShapes[i].CellCount <= _targetCellCount)
            .OrderByDescending(i => _availableShapes[i].CellCount)
            .ThenByDescending(i => GetShapeAsymmetry(_availableShapes[i]))
            .ToList();

        if (validIndices.Count == 0)
            return null;

        // 计算最少需要多少个形状才能填满
        int maxShapeSize = _availableShapes[validIndices[0]].CellCount;
        int minShapesNeeded = (_targetCellCount + maxShapeSize - 1) / maxShapeSize;
        
        // 如果最少需要的形状数超过限制，直接返回
        if (minShapesNeeded > _maxShapeCount)
            return null;

        // 迭代加深：从最少需要的形状数开始
        for (int maxDepth = minShapesNeeded; maxDepth <= _maxShapeCount; maxDepth++)
        {
            if ((DateTime.Now - _startTime).TotalMilliseconds > _maxSearchTime)
                break;
                
            var currentSelection = new List<int>();
            Search(validIndices, 0, currentSelection, 0, maxDepth);
            
            if (_bestSolution != null)
                return _bestSolution;
        }

        return _bestSolution;
    }

    /// <summary>
    /// 回溯搜索。允许同一形状被多次选择。
    /// </summary>
    private void Search(List<int> sortedIndices, int startIdx, List<int> current, int currentCellCount, int maxDepth)
    {
        // 超时检查
        if ((DateTime.Now - _startTime).TotalMilliseconds > _maxSearchTime)
            return;

        // 已找到解则停止
        if (_bestSolution != null)
            return;

        // 深度限制
        if (current.Count >= maxDepth)
            return;

        // 格数匹配，检查唯一性
        if (currentCellCount == _targetCellCount)
        {
            _searchCount++;
            if (CheckUniqueSolution(current))
            {
                _bestSolution = new List<int>(current);
            }
            return;
        }

        // 格数超出
        if (currentCellCount > _targetCellCount)
            return;

        int remainingCells = _targetCellCount - currentCellCount;
        int remainingSlots = maxDepth - current.Count;

        // 尝试添加每个形状
        for (int i = startIdx; i < sortedIndices.Count; i++)
        {
            int shapeIdx = sortedIndices[i];
            var shape = _availableShapes[shapeIdx];

            // 剪枝：如果当前形状太大，跳过
            if (shape.CellCount > remainingCells)
                continue;

            // 剪枝：即使全用最大的形状也填不满
            int maxFillable = shape.CellCount * remainingSlots;
            if (maxFillable < remainingCells)
            {
                _pruneCount++;
                continue;
            }

            // 剪枝：即使全用最小的形状也会超出
            int minShapeSize = _availableShapes[sortedIndices[sortedIndices.Count - 1]].CellCount;
            if (minShapeSize > 0 && remainingCells < minShapeSize)
            {
                _pruneCount++;
                break;
            }

            current.Add(shapeIdx);
            Search(sortedIndices, i, current, currentCellCount + shape.CellCount, maxDepth);
            current.RemoveAt(current.Count - 1);

            if (_bestSolution != null)
                return;
        }
    }

    /// <summary>
    /// 检查当前形状组合是否有唯一解。
    /// 先快速检查是否有解，再检查唯一性。
    /// </summary>
    private bool CheckUniqueSolution(List<int> shapeIndices)
    {
        var shapes = shapeIndices.Select(i => _availableShapes[i]).ToArray();
        var converter = new BoardToDLXConverter(_board, shapes);
        var matrix = converter.BuildMatrix();

        if (matrix.GetLength(0) == 0)
            return false;

        var solver = new ExactCoverSolver(matrix);
        
        // 快速检查是否有解
        var firstSolution = solver.SolveOne();
        if (firstSolution == null)
            return false;

        // 检查是否唯一
        var allSolutions = solver.SolveAll();
        int duplicateFactor = converter.GetDuplicateFactor();
        int uniqueCount = allSolutions.Count / duplicateFactor;

        return uniqueCount == 1;
    }

    /// <summary>
    /// 计算形状的不对称度（越不对称越好，更容易产生唯一解）。
    /// </summary>
    private static int GetShapeAsymmetry(ShapeData shape)
    {
        int asymmetry = 0;
        
        var rotations = new List<ShapeData> { shape };
        var current = shape;
        for (int i = 0; i < 3; i++)
        {
            current = current.RotateClockwise();
            bool isDuplicate = rotations.Any(r => ShapesEqual(r, current));
            if (!isDuplicate)
            {
                rotations.Add(current);
                asymmetry++;
            }
        }

        return asymmetry;
    }

    private static bool ShapesEqual(ShapeData a, ShapeData b)
    {
        if (a.Rows != b.Rows || a.Cols != b.Cols) return false;
        for (int r = 0; r < a.Rows; r++)
            for (int c = 0; c < a.Cols; c++)
                if (a.Matrix[r, c] != b.Matrix[r, c]) return false;
        return true;
    }
}
