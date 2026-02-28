namespace PictoMino.Core;

/// <summary>
/// 形状选择结果。
/// </summary>
public enum ShapeSelectResult
{
    /// <summary>找到唯一解</summary>
    Found,
    /// <summary>超时</summary>
    Timeout,
    /// <summary>目标太大（形状数超限）</summary>
    TargetTooLarge,
    /// <summary>无可用形状</summary>
    NoShapes,
    /// <summary>无有效放置</summary>
    NoValidPlacements,
    /// <summary>未找到唯一解</summary>
    NoUniqueSolution
}

/// <summary>
/// 形状选择的详细结果。
/// </summary>
public class ShapeSelectOutcome
{
    public ShapeSelectResult Result { get; init; }
    public List<int>? ShapeIndices { get; init; }
    public string Message { get; init; } = "";
    public int SearchCount { get; init; }
    public int PruneCount { get; init; }
    public int ElapsedMs { get; init; }

    public static ShapeSelectOutcome Success(List<int> indices, int searchCount, int pruneCount, int elapsedMs) => new()
    {
        Result = ShapeSelectResult.Found,
        ShapeIndices = indices,
        SearchCount = searchCount,
        PruneCount = pruneCount,
        ElapsedMs = elapsedMs
    };

    public static ShapeSelectOutcome Failure(ShapeSelectResult result, string message, int searchCount = 0, int pruneCount = 0, int elapsedMs = 0) => new()
    {
        Result = result,
        Message = message,
        SearchCount = searchCount,
        PruneCount = pruneCount,
        ElapsedMs = elapsedMs
    };
}

/// <summary>
/// 自动选择形状以填满目标图案并保证唯一解。
/// 使用回溯搜索 + DLX验证，优化启发式和剪枝。
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
    private DateTime _deadline;
    private bool _timedOut;
    private int _checkCounter;

    /// <summary>
    /// 形状选择信息，用于启发式排序和剪枝。
    /// </summary>
    private readonly record struct ShapeInfo(
        int OriginalIndex,
        ShapeData Shape,
        int ValidPlacementCount,
        int UniqueRotationCount);

    private ShapeInfo[]? _shapeInfos;

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
        var outcome = FindUniqueSolutionWithDetails();
        return outcome.ShapeIndices;
    }

    /// <summary>
    /// 搜索能产生唯一解的形状组合，返回详细结果。
    /// </summary>
    public ShapeSelectOutcome FindUniqueSolutionWithDetails()
    {
        _bestSolution = null;
        _searchCount = 0;
        _pruneCount = 0;
        _checkCounter = 0;
        _timedOut = false;
        _startTime = DateTime.Now;
        _deadline = _startTime.AddMilliseconds(_maxSearchTime);

        // 检查是否有可用形状
        if (_availableShapes == null || _availableShapes.Length == 0)
        {
            return ShapeSelectOutcome.Failure(ShapeSelectResult.NoShapes, "没有可用的形状");
        }

        // 预计算形状信息
        if (!PrecomputeShapeInfos())
        {
            int precomputeElapsed = (int)(DateTime.Now - _startTime).TotalMilliseconds;
            return ShapeSelectOutcome.Failure(ShapeSelectResult.Timeout,
                $"预计算阶段超时（{precomputeElapsed}ms）",
                _searchCount, _pruneCount, precomputeElapsed);
        }
        
        if (_shapeInfos == null || _shapeInfos.Length == 0)
        {
            return ShapeSelectOutcome.Failure(ShapeSelectResult.NoValidPlacements, 
                $"所有 {_availableShapes.Length} 个形状都无法放入目标区域");
        }

        // 计算最少需要多少个形状才能填满
        int maxShapeSize = _shapeInfos.Max(s => s.Shape.CellCount);
        int minShapesNeeded = (_targetCellCount + maxShapeSize - 1) / maxShapeSize;
        
        // 如果最少需要的形状数超过限制
        if (minShapesNeeded > _maxShapeCount)
        {
            return ShapeSelectOutcome.Failure(ShapeSelectResult.TargetTooLarge,
                $"目标有 {_targetCellCount} 格，最大形状 {maxShapeSize} 格，" +
                $"至少需要 {minShapesNeeded} 个形状，但限制为 {_maxShapeCount} 个");
        }

        // 迭代加深：从最少需要的形状数开始
        for (int maxDepth = minShapesNeeded; maxDepth <= _maxShapeCount; maxDepth++)
        {
            // 使用 _timedOut 标志，避免每次都调用 DateTime.Now
            if (_timedOut || DateTime.Now > _deadline)
            {
                _timedOut = true;
                break;
            }
                
            var currentSelection = new List<int>();
            var usedCells = new HashSet<(int, int)>();
            Search(0, currentSelection, 0, 0, maxDepth, usedCells);
            
            if (_bestSolution != null)
            {
                return ShapeSelectOutcome.Success(_bestSolution, _searchCount, _pruneCount,
                    (int)(DateTime.Now - _startTime).TotalMilliseconds);
            }
        }

        int elapsedMs = (int)(DateTime.Now - _startTime).TotalMilliseconds;
        
        if (_timedOut || elapsedMs >= _maxSearchTime)
        {
            return ShapeSelectOutcome.Failure(ShapeSelectResult.Timeout,
                $"搜索超时（{_maxSearchTime}ms），已检查 {_searchCount} 个组合，剪枝 {_pruneCount} 次",
                _searchCount, _pruneCount, elapsedMs);
        }

        return ShapeSelectOutcome.Failure(ShapeSelectResult.NoUniqueSolution,
            $"已检查 {_searchCount} 个组合，未找到唯一解",
            _searchCount, _pruneCount, elapsedMs);
    }

    /// <summary>
    /// 预计算形状信息，用于启发式排序和剪枝。
    /// </summary>
    /// <returns>是否在超时前完成</returns>
    private bool PrecomputeShapeInfos()
    {
        // 收集目标格子
        var targetCells = new List<(int r, int c)>();
        var cellIndexMap = new Dictionary<(int, int), int>();
        int cellIdx = 0;
        
        for (int r = 0; r < _board.Rows; r++)
        {
            for (int c = 0; c < _board.Cols; c++)
            {
                if (_board.Target?[r, c] ?? true)
                {
                    targetCells.Add((r, c));
                    cellIndexMap[(r, c)] = cellIdx++;
                }
            }
        }

        // 计算每个形状的有效放置数
        var infos = new List<ShapeInfo>();
        
        for (int i = 0; i < _availableShapes.Length; i++)
        {
            // 每个形状处理前检查超时
            if (DateTime.Now > _deadline)
            {
                _shapeInfos = null;
                _timedOut = true;
                return false;
            }

            var shape = _availableShapes[i];
            
            // 形状太大，跳过
            if (shape.CellCount > _targetCellCount)
                continue;

            int validPlacements = 0;
            var rotations = GetUniqueRotations(shape);

            foreach (var rot in rotations)
            {
                // 每个旋转处理前检查超时
                if (DateTime.Now > _deadline)
                {
                    _shapeInfos = null;
                    _timedOut = true;
                    return false;
                }

                for (int r = 0; r <= _board.Rows - rot.Rows; r++)
                {
                    for (int c = 0; c <= _board.Cols - rot.Cols; c++)
                    {
                        if (IsValidPlacement(rot, r, c, cellIndexMap))
                        {
                            validPlacements++;
                        }
                    }
                }
            }

            // 无有效放置的形状被排除
            if (validPlacements > 0)
            {
                infos.Add(new ShapeInfo(i, shape, validPlacements, rotations.Count));
            }
        }

        // 按约束程度排序：放置方式少的优先（更约束）
        _shapeInfos = infos
            .OrderBy(s => s.ValidPlacementCount)
            .ThenByDescending(s => s.UniqueRotationCount)
            .ThenByDescending(s => s.Shape.CellCount)
            .ToArray();
        
        return true;
    }

    /// <summary>
    /// 回溯搜索。允许同一形状被多次选择。
    /// </summary>
    private void Search(int startIdx, List<int> current, int currentCellCount, int currentPlacements, 
                        int maxDepth, HashSet<(int, int)> usedCells)
    {
        // 快速检查：如果已超时则立即返回
        if (_timedOut)
            return;

        // 每 100 次检查一次实际时间
        if (++_checkCounter % 100 == 0)
        {
            if (DateTime.Now > _deadline)
            {
                _timedOut = true;
                return;
            }
        }

        // 已找到解则停止
        if (_bestSolution != null)
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

        // 深度限制：如果已经用了 maxDepth 个形状，不能再加
        if (current.Count >= maxDepth)
            return;

        int remainingCells = _targetCellCount - currentCellCount;
        int remainingSlots = maxDepth - current.Count;

        // 剪枝：即使全用最大的形状也填不满
        int maxShapeSize = _shapeInfos!.Max(s => s.Shape.CellCount);
        if (maxShapeSize * remainingSlots < remainingCells)
        {
            _pruneCount++;
            return;
        }

        // 尝试添加每个形状
        for (int i = startIdx; i < _shapeInfos!.Length; i++)
        {
            // 快速超时检查
            if (_timedOut)
                return;

            ref readonly var info = ref _shapeInfos[i];
            int shapeIdx = info.OriginalIndex;
            var shape = info.Shape;

            // 剪枝：如果当前形状太大，跳过
            if (shape.CellCount > remainingCells)
                continue;

            // 剪枝：即使全用当前形状也填不满
            if (shape.CellCount * remainingSlots < remainingCells)
            {
                _pruneCount++;
                break; // 后面的形状更小或相同大小
            }

            current.Add(shapeIdx);
            Search(i, current, currentCellCount + shape.CellCount, 
                   currentPlacements + info.ValidPlacementCount, maxDepth, usedCells);
            current.RemoveAt(current.Count - 1);

            if (_bestSolution != null)
                return;
        }
    }

    /// <summary>
    /// 检查当前形状组合是否有唯一解。
    /// </summary>
    private bool CheckUniqueSolution(List<int> shapeIndices)
    {
        // 超时检查
        if (_timedOut || DateTime.Now > _deadline)
        {
            _timedOut = true;
            return false;
        }

        var shapes = shapeIndices.Select(i => _availableShapes[i]).ToArray();
        var converter = new BoardToDLXConverter(_board, shapes);
        var matrix = converter.BuildMatrix(_deadline);

        // 检查是否超时或无放置
        if (converter.TimedOut || matrix.GetLength(0) == 0)
        {
            _timedOut = converter.TimedOut;
            return false;
        }

        var solver = new ExactCoverSolver(matrix, _deadline);
        
        // 检查是否构建超时
        if (solver.TimedOut)
        {
            _timedOut = true;
            return false;
        }
        
        // 快速检查是否有解
        var firstSolution = solver.SolveOne(_deadline);
        if (firstSolution == null)
            return false;

        // 检查是否唯一：只需找到最多 duplicateFactor + 1 个解
        int duplicateFactor = converter.GetDuplicateFactor();
        var solutions = solver.SolveAll(duplicateFactor + 1, _deadline);
        int uniqueCount = solutions.Count / duplicateFactor;

        return uniqueCount == 1;
    }

    /// <summary>
    /// 获取形状的所有唯一旋转变体。
    /// </summary>
    private static List<ShapeData> GetUniqueRotations(ShapeData shape)
    {
        var rotations = new List<ShapeData> { shape };
        var current = shape;

        for (int i = 0; i < 3; i++)
        {
            current = current.RotateClockwise();
            if (!rotations.Any(r => ShapesEqual(r, current)))
            {
                rotations.Add(current);
            }
        }

        return rotations;
    }

    private static bool ShapesEqual(ShapeData a, ShapeData b)
    {
        if (a.Rows != b.Rows || a.Cols != b.Cols) return false;
        for (int r = 0; r < a.Rows; r++)
            for (int c = 0; c < a.Cols; c++)
                if (a.Matrix[r, c] != b.Matrix[r, c]) return false;
        return true;
    }

    /// <summary>
    /// 检查放置是否有效。
    /// </summary>
    private bool IsValidPlacement(ShapeData shape, int row, int col, Dictionary<(int, int), int> cellIndexMap)
    {
        for (int sr = 0; sr < shape.Rows; sr++)
        {
            for (int sc = 0; sc < shape.Cols; sc++)
            {
                if (!shape.Matrix[sr, sc]) continue;

                int boardRow = row + sr;
                int boardCol = col + sc;

                if (!_board.IsInBounds(boardRow, boardCol))
                    return false;

                if (!cellIndexMap.ContainsKey((boardRow, boardCol)))
                    return false;
            }
        }
        return true;
    }
}