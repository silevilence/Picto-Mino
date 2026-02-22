namespace PictoMino.Core.Tests;

/// <summary>
/// 精确覆盖问题 (Exact Cover) 求解器测试。
/// </summary>
[TestFixture]
public class ExactCoverSolverTests
{
    // ─── 基础矩阵构建 ───────────────────────────────────

    [Test]
    public void BuildMatrix_CreatesCorrectStructure()
    {
        // 简单的 3 列 2 行矩阵:
        // Row 0: [1, 0, 1]
        // Row 1: [0, 1, 0]
        var matrix = new int[,]
        {
            { 1, 0, 1 },
            { 0, 1, 0 }
        };

        var solver = new ExactCoverSolver(matrix);

        Assert.That(solver.ColumnCount, Is.EqualTo(3));
        Assert.That(solver.RowCount, Is.EqualTo(2));
    }

    [Test]
    public void BuildMatrix_EmptyMatrix_Throws()
    {
        var matrix = new int[0, 0];

        Assert.Throws<ArgumentException>(() => new ExactCoverSolver(matrix));
    }

    // ─── 简单精确覆盖求解 ──────────────────────────────────

    [Test]
    public void Solve_TrivialCase_FindsSolution()
    {
        // 单行覆盖所有列
        // Row 0: [1, 1, 1]
        var matrix = new int[,]
        {
            { 1, 1, 1 }
        };

        var solver = new ExactCoverSolver(matrix);
        var solutions = solver.SolveAll();

        Assert.That(solutions, Has.Count.EqualTo(1));
        Assert.That(solutions[0], Is.EquivalentTo(new[] { 0 }));
    }

    [Test]
    public void Solve_NoSolution_ReturnsEmpty()
    {
        // 两行都覆盖同一列，无法同时选择
        // Row 0: [1, 0]
        // Row 1: [1, 0]
        // 列 1 无法被覆盖
        var matrix = new int[,]
        {
            { 1, 0 },
            { 1, 0 }
        };

        var solver = new ExactCoverSolver(matrix);
        var solutions = solver.SolveAll();

        Assert.That(solutions, Is.Empty);
    }

    [Test]
    public void Solve_ClassicExample_FindsSolution()
    {
        // 经典精确覆盖示例:
        // 列:    0  1  2  3  4  5  6
        // Row 0: 1  0  0  1  0  0  1
        // Row 1: 1  0  0  1  0  0  0
        // Row 2: 0  0  0  1  1  0  1
        // Row 3: 0  0  1  0  1  1  0
        // Row 4: 0  1  1  0  0  1  1
        // Row 5: 0  1  0  0  0  0  1
        // 解: 行 1, 3, 5 (或 行 0, 3, 5 等)
        var matrix = new int[,]
        {
            { 1, 0, 0, 1, 0, 0, 1 },
            { 1, 0, 0, 1, 0, 0, 0 },
            { 0, 0, 0, 1, 1, 0, 1 },
            { 0, 0, 1, 0, 1, 1, 0 },
            { 0, 1, 1, 0, 0, 1, 1 },
            { 0, 1, 0, 0, 0, 0, 1 }
        };

        var solver = new ExactCoverSolver(matrix);
        var solutions = solver.SolveAll();

        Assert.That(solutions.Count, Is.GreaterThan(0));

        // 验证找到的解确实覆盖所有列恰好一次
        foreach (var solution in solutions)
        {
            var covered = new int[7];
            foreach (var rowIndex in solution)
            {
                for (int col = 0; col < 7; col++)
                {
                    covered[col] += matrix[rowIndex, col];
                }
            }
            Assert.That(covered, Is.All.EqualTo(1));
        }
    }

    [Test]
    public void Solve_MultipleSolutions_FindsAll()
    {
        // 两种方式覆盖:
        // Row 0: [1, 0]
        // Row 1: [0, 1]
        // Row 2: [1, 1]
        // 解: {0, 1} 或 {2}
        var matrix = new int[,]
        {
            { 1, 0 },
            { 0, 1 },
            { 1, 1 }
        };

        var solver = new ExactCoverSolver(matrix);
        var solutions = solver.SolveAll();

        Assert.That(solutions, Has.Count.EqualTo(2));
    }

    [Test]
    public void SolveOne_FindsFirstSolution()
    {
        var matrix = new int[,]
        {
            { 1, 0 },
            { 0, 1 },
            { 1, 1 }
        };

        var solver = new ExactCoverSolver(matrix);
        var solution = solver.SolveOne();

        Assert.That(solution, Is.Not.Null);
        Assert.That(solution!.Count, Is.GreaterThan(0));
    }

    [Test]
    public void HasSolution_ReturnsTrueWhenSolvable()
    {
        var matrix = new int[,]
        {
            { 1, 1, 1 }
        };

        var solver = new ExactCoverSolver(matrix);

        Assert.That(solver.HasSolution(), Is.True);
    }

    [Test]
    public void HasSolution_ReturnsFalseWhenUnsolvable()
    {
        var matrix = new int[,]
        {
            { 1, 0 },
            { 1, 0 }
        };

        var solver = new ExactCoverSolver(matrix);

        Assert.That(solver.HasSolution(), Is.False);
    }

    // ─── 性能与边界 ─────────────────────────────────────

    [Test]
    public void Solve_LargerMatrix_CompletesInReasonableTime()
    {
        // 5x5 对角矩阵，唯一解
        var matrix = new int[5, 5];
        for (int i = 0; i < 5; i++)
            matrix[i, i] = 1;

        var solver = new ExactCoverSolver(matrix);
        var solutions = solver.SolveAll();

        Assert.That(solutions, Has.Count.EqualTo(1));
        Assert.That(solutions[0], Is.EquivalentTo(new[] { 0, 1, 2, 3, 4 }));
    }

    [Test]
    public void CountSolutions_ReturnsCorrectCount()
    {
        var matrix = new int[,]
        {
            { 1, 0 },
            { 0, 1 },
            { 1, 1 }
        };

        var solver = new ExactCoverSolver(matrix);

        Assert.That(solver.CountSolutions(), Is.EqualTo(2));
    }
}
