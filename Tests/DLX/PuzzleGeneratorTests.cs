namespace PictoMino.Core.Tests;

/// <summary>
/// 关卡生成器测试。
/// </summary>
[TestFixture]
public class PuzzleGeneratorTests
{
    // ─── 基础生成 ────────────────────────────────────────

    [Test]
    public void Generate_SimpleCase_ReturnsSolvablePuzzle()
    {
        // 2x2 棋盘，使用 4 个单格形状（一定可解）
        var shapes = new[]
        {
            new ShapeData(new bool[,] { { true } }),
            new ShapeData(new bool[,] { { true } }),
            new ShapeData(new bool[,] { { true } }),
            new ShapeData(new bool[,] { { true } })
        };

        var generator = new PuzzleGenerator(2, 2);
        var puzzle = generator.GenerateWithShapes(shapes, seed: 42);

        Assert.That(puzzle, Is.Not.Null);
        Assert.That(puzzle!.Board.Rows, Is.EqualTo(2));
        Assert.That(puzzle.Board.Cols, Is.EqualTo(2));
        Assert.That(puzzle.Shapes, Has.Length.EqualTo(4));
    }

    [Test]
    public void Generate_VerifySolvable_OneSolution()
    {
        // 生成的谜题必须有解
        var shapes = new[]
        {
            new ShapeData(new bool[,] { { true, true } }), // 2格横条
            new ShapeData(new bool[,] { { true, true } }), // 2格横条
        };

        var generator = new PuzzleGenerator(2, 2);
        var puzzle = generator.GenerateWithShapes(shapes, seed: 123);

        Assert.That(puzzle, Is.Not.Null);

        // 验证可解
        var converter = new BoardToDLXConverter(puzzle!.Board, puzzle.Shapes);
        var matrix = converter.BuildMatrix();
        var solver = new ExactCoverSolver(matrix);

        Assert.That(solver.HasSolution(), Is.True);
    }

    [Test]
    public void Generate_UniqueSolution_OnlyOneAnswer()
    {
        // 使用 2x2 方块，只有一种放法，保证唯一解
        var shapes = new[]
        {
            new ShapeData(new bool[,] { { true, true }, { true, true } }), // O形 2x2
        };

        var generator = new PuzzleGenerator(2, 2);
        var puzzle = generator.GenerateUnique(shapes, seed: 456);

        Assert.That(puzzle, Is.Not.Null);

        // 验证唯一解
        var converter = new BoardToDLXConverter(puzzle!.Board, puzzle.Shapes);
        var matrix = converter.BuildMatrix();
        var solver = new ExactCoverSolver(matrix);

        Assert.That(solver.CountSolutions(), Is.EqualTo(1));
    }

    // ─── 随机生成 ────────────────────────────────────────

    [Test]
    public void GenerateRandom_CreatesValidPuzzle()
    {
        var generator = new PuzzleGenerator(4, 4);
        var puzzle = generator.GenerateRandom(minShapes: 2, maxShapes: 4, seed: 789);

        Assert.That(puzzle, Is.Not.Null);
        Assert.That(puzzle!.Shapes.Length, Is.InRange(2, 4));
    }

    [Test]
    public void GenerateRandom_DifferentSeeds_DifferentPuzzles()
    {
        var generator = new PuzzleGenerator(4, 4);
        var puzzle1 = generator.GenerateRandom(minShapes: 2, maxShapes: 3, seed: 100);
        var puzzle2 = generator.GenerateRandom(minShapes: 2, maxShapes: 3, seed: 200);

        Assert.That(puzzle1, Is.Not.Null);
        Assert.That(puzzle2, Is.Not.Null);

        // 不同种子应产生不同谜题（至少形状或目标不同）
        bool areDifferent = puzzle1!.Shapes.Length != puzzle2!.Shapes.Length ||
            !TargetsEqual(puzzle1.Board.Target, puzzle2.Board.Target);

        Assert.That(areDifferent, Is.True);
    }

    // ─── 目标图案生成 ───────────────────────────────────

    [Test]
    public void GenerateWithTarget_UsesProvidedTarget()
    {
        var target = new bool[,]
        {
            { true, true },
            { true, false }
        };

        var shapes = new[]
        {
            new ShapeData(new bool[,] { { true } }),
            new ShapeData(new bool[,] { { true, true } }),
        };

        var generator = new PuzzleGenerator(2, 2);
        var puzzle = generator.GenerateWithTarget(target, shapes);

        Assert.That(puzzle, Is.Not.Null);
        Assert.That(puzzle!.Board.Target, Is.Not.Null);
        Assert.That(puzzle.Board.Target![0, 0], Is.True);
        Assert.That(puzzle.Board.Target[1, 1], Is.False);
    }

    // ─── 边界情况 ────────────────────────────────────────

    [Test]
    public void Generate_ImpossibleConstraints_ReturnsNull()
    {
        // 无法满足的约束：5x5 棋盘但只有一个小形状
        var shapes = new[]
        {
            new ShapeData(new bool[,] { { true } })
        };

        var generator = new PuzzleGenerator(5, 5);
        var puzzle = generator.GenerateWithShapes(shapes);

        // 一个1格形状无法填满25格
        Assert.That(puzzle, Is.Null);
    }

    [Test]
    public void Generate_EmptyShapeList_ReturnsNull()
    {
        var generator = new PuzzleGenerator(3, 3);
        var puzzle = generator.GenerateWithShapes(Array.Empty<ShapeData>());

        Assert.That(puzzle, Is.Null);
    }

    // ─── 辅助方法 ────────────────────────────────────────

    private static bool TargetsEqual(bool[,]? a, bool[,]? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
            return false;

        for (int r = 0; r < a.GetLength(0); r++)
            for (int c = 0; c < a.GetLength(1); c++)
                if (a[r, c] != b[r, c]) return false;

        return true;
    }
}
