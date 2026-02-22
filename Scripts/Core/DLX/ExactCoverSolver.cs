namespace PictoMino.Core;

/// <summary>
/// 基于 Dancing Links 的精确覆盖问题 (Exact Cover) 求解器。
/// 
/// 精确覆盖问题：给定一个 0-1 矩阵，选择若干行使得每列恰好有一个 1。
/// </summary>
public class ExactCoverSolver
{
    private readonly DLXNode _header; // 虚拟头节点
    private readonly DLXColumn[] _columns;
    private readonly List<int> _currentSolution;
    private readonly List<List<int>> _allSolutions;
    private bool _stopAtFirst;

    /// <summary>矩阵列数</summary>
    public int ColumnCount { get; }

    /// <summary>矩阵行数</summary>
    public int RowCount { get; }

    /// <summary>
    /// 从 0-1 矩阵构建 DLX 数据结构。
    /// </summary>
    /// <param name="matrix">0-1 矩阵，matrix[row, col] 为 1 表示该行覆盖该列</param>
    public ExactCoverSolver(int[,] matrix)
    {
        RowCount = matrix.GetLength(0);
        ColumnCount = matrix.GetLength(1);

        if (RowCount == 0 || ColumnCount == 0)
            throw new ArgumentException("Matrix cannot be empty.");

        _header = new DLXNode();
        _columns = new DLXColumn[ColumnCount];
        _currentSolution = new List<int>();
        _allSolutions = new List<List<int>>();

        BuildStructure(matrix);
    }

    /// <summary>
    /// 构建 Dancing Links 数据结构。
    /// </summary>
    private void BuildStructure(int[,] matrix)
    {
        // 创建列头节点并链接到 header
        for (int c = 0; c < ColumnCount; c++)
        {
            var column = new DLXColumn(c, $"C{c}");
            _columns[c] = column;
            column.InsertRight(_header.Left); // 插入到 header 的左侧（即末尾）
        }

        // 创建每行的节点
        for (int r = 0; r < RowCount; r++)
        {
            DLXNode? rowFirst = null;

            for (int c = 0; c < ColumnCount; c++)
            {
                if (matrix[r, c] != 1) continue;

                var column = _columns[c];
                var node = new DLXNode
                {
                    Column = column,
                    RowIndex = r
                };

                // 垂直链接：插入到列的底部
                node.InsertDown(column.Up);
                column.Size++;

                // 水平链接：同一行的节点
                if (rowFirst == null)
                {
                    rowFirst = node;
                }
                else
                {
                    node.InsertRight(rowFirst.Left); // 插入到行的末尾
                }
            }
        }
    }

    /// <summary>
    /// 查找所有解。
    /// </summary>
    /// <returns>所有解的列表，每个解是选中行索引的列表</returns>
    public List<List<int>> SolveAll()
    {
        _allSolutions.Clear();
        _currentSolution.Clear();
        _stopAtFirst = false;
        Search();
        return new List<List<int>>(_allSolutions);
    }

    /// <summary>
    /// 查找第一个解。
    /// </summary>
    /// <returns>第一个解（选中行索引的列表），无解返回 null</returns>
    public List<int>? SolveOne()
    {
        _allSolutions.Clear();
        _currentSolution.Clear();
        _stopAtFirst = true;
        Search();
        return _allSolutions.Count > 0 ? _allSolutions[0] : null;
    }

    /// <summary>
    /// 判断是否存在解。
    /// </summary>
    public bool HasSolution()
    {
        return SolveOne() != null;
    }

    /// <summary>
    /// 统计解的数量。
    /// </summary>
    public int CountSolutions()
    {
        return SolveAll().Count;
    }

    /// <summary>
    /// Algorithm X 递归搜索。
    /// </summary>
    private void Search()
    {
        // 若已找到解且只需一个解，则停止
        if (_stopAtFirst && _allSolutions.Count > 0)
            return;

        // 若所有列都被覆盖，找到一个解
        if (_header.Right == _header)
        {
            _allSolutions.Add(new List<int>(_currentSolution));
            return;
        }

        // 选择节点数最少的列（MRV 启发式）
        var column = ChooseColumn();
        if (column == null || column.Size == 0)
            return; // 死路，回溯

        column.CoverColumn();

        // 遍历该列的每一行
        for (var row = column.Down; row != column; row = row.Down)
        {
            _currentSolution.Add(row.RowIndex);

            // 覆盖该行涉及的所有其他列
            for (var node = row.Right; node != row; node = node.Right)
            {
                node.Column!.CoverColumn();
            }

            Search();

            // 回溯：恢复该行涉及的所有其他列（逆序）
            for (var node = row.Left; node != row; node = node.Left)
            {
                node.Column!.UncoverColumn();
            }

            _currentSolution.RemoveAt(_currentSolution.Count - 1);

            if (_stopAtFirst && _allSolutions.Count > 0)
            {
                column.UncoverColumn();
                return;
            }
        }

        column.UncoverColumn();
    }

    /// <summary>
    /// 选择节点数最少的列（最小剩余值启发式）。
    /// </summary>
    private DLXColumn? ChooseColumn()
    {
        DLXColumn? best = null;
        int minSize = int.MaxValue;

        for (var node = _header.Right; node != _header; node = node.Right)
        {
            var column = (DLXColumn)node;
            if (column.Size < minSize)
            {
                minSize = column.Size;
                best = column;
            }
        }

        return best;
    }
}
