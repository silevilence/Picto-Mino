namespace PictoMino.Core;

/// <summary>
/// 表示生成的谜题。
/// </summary>
public record PuzzleData(BoardData Board, ShapeData[] Shapes);

/// <summary>
/// 关卡谜题生成器。
/// 使用 DLX 算法验证谜题可解性。
/// </summary>
public class PuzzleGenerator
{
    private readonly int _rows;
    private readonly int _cols;

    // 预定义的标准多米诺形状库
    private static readonly bool[][,] StandardShapes = new[]
    {
        // 单格
        new bool[,] { { true } },
        // 双格
        new bool[,] { { true, true } },
        new bool[,] { { true }, { true } },
        // 三格
        new bool[,] { { true, true, true } },
        new bool[,] { { true }, { true }, { true } },
        new bool[,] { { true, true }, { true, false } },
        new bool[,] { { true, true }, { false, true } },
        new bool[,] { { true, false }, { true, true } },
        new bool[,] { { false, true }, { true, true } },
        // 四格
        new bool[,] { { true, true, true, true } },
        new bool[,] { { true }, { true }, { true }, { true } },
        new bool[,] { { true, true }, { true, true } }, // 方块
        new bool[,] { { true, true, true }, { true, false, false } }, // L
        new bool[,] { { true, true, true }, { false, false, true } }, // J
        new bool[,] { { true, true, false }, { false, true, true } }, // S
        new bool[,] { { false, true, true }, { true, true, false } }, // Z
        new bool[,] { { true, true, true }, { false, true, false } }, // T
    };

    public PuzzleGenerator(int rows, int cols)
    {
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (cols <= 0) throw new ArgumentOutOfRangeException(nameof(cols));

        _rows = rows;
        _cols = cols;
    }

    /// <summary>
    /// 使用指定形状生成谜题。
    /// </summary>
    /// <param name="shapes">可用形状列表</param>
    /// <param name="seed">随机种子（可选）</param>
    /// <returns>谜题，或 null 如果无法生成</returns>
    public PuzzleData? GenerateWithShapes(ShapeData[] shapes, int? seed = null)
    {
        if (shapes.Length == 0) return null;

        // 计算形状总格数
        int totalCells = shapes.Sum(s => s.CellCount);
        int boardCells = _rows * _cols;

        if (totalCells != boardCells)
            return null; // 格数不匹配

        var board = new BoardData(_rows, _cols);
        return TryGenerateSolvable(board, shapes) ? new PuzzleData(board, shapes) : null;
    }

    /// <summary>
    /// 生成具有唯一解的谜题。
    /// </summary>
    public PuzzleData? GenerateUnique(ShapeData[] shapes, int? seed = null)
    {
        if (shapes.Length == 0) return null;

        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        // 尝试多次生成唯一解谜题
        for (int attempt = 0; attempt < 100; attempt++)
        {
            int totalCells = shapes.Sum(s => s.CellCount);
            var target = GenerateRandomTarget(totalCells, rng);
            var board = new BoardData(_rows, _cols, target);

            var converter = new BoardToDLXConverter(board, shapes);
            var matrix = converter.BuildMatrix();

            if (matrix.GetLength(0) == 0) continue;

            var solver = new ExactCoverSolver(matrix);
            if (solver.CountSolutions() == 1)
            {
                return new PuzzleData(board, shapes);
            }
        }

        // 退化到全填充
        var fullBoard = new BoardData(_rows, _cols);
        if (TryGenerateSolvable(fullBoard, shapes))
        {
            var converter = new BoardToDLXConverter(fullBoard, shapes);
            var matrix = converter.BuildMatrix();
            var solver = new ExactCoverSolver(matrix);
            if (solver.CountSolutions() == 1)
            {
                return new PuzzleData(fullBoard, shapes);
            }
        }

        return null;
    }

    /// <summary>
    /// 随机生成谜题。
    /// </summary>
    public PuzzleData? GenerateRandom(int minShapes, int maxShapes, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        for (int attempt = 0; attempt < 100; attempt++)
        {
            // 随机选择形状组合
            int numShapes = rng.Next(minShapes, maxShapes + 1);
            var selectedShapes = SelectRandomShapes(numShapes, rng);

            int totalCells = selectedShapes.Sum(s => s.CellCount);

            if (totalCells > _rows * _cols) continue;

            // 生成匹配的目标图案
            var target = GenerateRandomTarget(totalCells, rng);
            var board = new BoardData(_rows, _cols, target);

            // 验证可解
            var converter = new BoardToDLXConverter(board, selectedShapes);
            var matrix = converter.BuildMatrix();

            if (matrix.GetLength(0) == 0) continue;

            var solver = new ExactCoverSolver(matrix);
            if (solver.HasSolution())
            {
                return new PuzzleData(board, selectedShapes);
            }
        }

        return null;
    }

    /// <summary>
    /// 使用指定目标图案生成谜题。
    /// </summary>
    public PuzzleData? GenerateWithTarget(bool[,] target, ShapeData[] shapes)
    {
        if (shapes.Length == 0) return null;

        var board = new BoardData(_rows, _cols, target);

        // 验证目标格数与形状格数匹配
        int targetCells = 0;
        for (int r = 0; r < _rows; r++)
            for (int c = 0; c < _cols; c++)
                if (target[r, c]) targetCells++;

        int shapeCells = shapes.Sum(s => s.CellCount);
        if (targetCells != shapeCells) return null;

        // 验证可解
        var converter = new BoardToDLXConverter(board, shapes);
        var matrix = converter.BuildMatrix();

        if (matrix.GetLength(0) == 0) return null;

        var solver = new ExactCoverSolver(matrix);
        return solver.HasSolution() ? new PuzzleData(board, shapes) : null;
    }

    /// <summary>
    /// 验证棋盘和形状组合是否可解。
    /// </summary>
    private bool TryGenerateSolvable(BoardData board, ShapeData[] shapes)
    {
        var converter = new BoardToDLXConverter(board, shapes);
        var matrix = converter.BuildMatrix();

        if (matrix.GetLength(0) == 0) return false;

        var solver = new ExactCoverSolver(matrix);
        return solver.HasSolution();
    }

    /// <summary>
    /// 生成随机目标图案。
    /// </summary>
    private bool[,] GenerateRandomTarget(int cellCount, Random rng)
    {
        var target = new bool[_rows, _cols];
        var positions = new List<(int r, int c)>();

        for (int r = 0; r < _rows; r++)
            for (int c = 0; c < _cols; c++)
                positions.Add((r, c));

        // 随机打乱
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        // 选择前 cellCount 个
        for (int i = 0; i < Math.Min(cellCount, positions.Count); i++)
        {
            var (r, c) = positions[i];
            target[r, c] = true;
        }

        return target;
    }

    /// <summary>
    /// 随机选择形状。
    /// </summary>
    private ShapeData[] SelectRandomShapes(int count, Random rng)
    {
        var result = new ShapeData[count];

        for (int i = 0; i < count; i++)
        {
            int idx = rng.Next(StandardShapes.Length);
            result[i] = new ShapeData(StandardShapes[idx]);
        }

        return result;
    }
}
