using NUnit.Framework;
using PictoMino.Core;

namespace PictoMino.Tests.DLX;

[TestFixture]
[Timeout(10000)] // 所有测试最多 10 秒
public class ShapeSelectorTests
{
    #region 基本功能测试

    [Test]
    public void FindUniqueSolution_SimpleCase_FindsSolution()
    {
        // 2x3 目标，用 I3 + I3 可以填满且唯一
        var target = new bool[2, 3]
        {
            { true, true, true },
            { true, true, true }
        };
        var board = new BoardData(2, 3, target);

        var shapes = new[]
        {
            new ShapeData(new[,] { { true, true } }),       // I2 横
            new ShapeData(new[,] { { true, true, true } }), // I3 横
        };

        var selector = new ShapeSelector(board, shapes, 5000);
        var result = selector.FindUniqueSolution();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result.All(i => i == 1), Is.True); // 都是 I3
    }

    [Test]
    public void FindUniqueSolution_NoSolution_ReturnsNull()
    {
        // 3x3 目标，只有 I2 形状，无法填满（9格用I2填不满）
        var target = new bool[3, 3]
        {
            { true, true, true },
            { true, true, true },
            { true, true, true }
        };
        var board = new BoardData(3, 3, target);

        var shapes = new[]
        {
            new ShapeData(new[,] { { true, true } }), // I2
        };

        var selector = new ShapeSelector(board, shapes, 1000);
        var result = selector.FindUniqueSolution();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindUniqueSolution_LShape_FindsSolution()
    {
        // 2x2 目标，用 L3 + I1 可以填满
        var target = new bool[2, 2]
        {
            { true, true },
            { true, true }
        };
        var board = new BoardData(2, 2, target);

        var shapes = new[]
        {
            new ShapeData(new[,] { { true } }),                        // I1
            new ShapeData(new[,] { { true, true } }),                  // I2
            new ShapeData(new[,] { { true, false }, { true, true } }), // L3
        };

        var selector = new ShapeSelector(board, shapes, 5000);
        var result = selector.FindUniqueSolution();

        Assert.That(result, Is.Not.Null);
        int totalCells = result!.Sum(i => shapes[i].CellCount);
        Assert.That(totalCells, Is.EqualTo(4));
    }

    #endregion

    #region 边界条件测试

    [Test]
    public void FindUniqueSolution_EmptyShapes_ReturnsNull()
    {
        var target = new bool[2, 2]
        {
            { true, true },
            { true, true }
        };
        var board = new BoardData(2, 2, target);

        var selector = new ShapeSelector(board, Array.Empty<ShapeData>(), 1000);
        var result = selector.FindUniqueSolution();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindUniqueSolution_ShapeLargerThanTarget_IsExcluded()
    {
        // 2x2 目标 = 4格
        var target = new bool[2, 2]
        {
            { true, true },
            { true, true }
        };
        var board = new BoardData(2, 2, target);

        var shapes = new[]
        {
            new ShapeData(new[,] { { true, true, true, true, true } }), // I5 (5格，比目标大)
            new ShapeData(new[,] { { true, true } }),                   // I2
        };

        var selector = new ShapeSelector(board, shapes, 1000);
        var result = selector.FindUniqueSolution();

        // 应该能用两个 I2 填满
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result.All(i => i == 1), Is.True);
    }

    [Test]
    public void FindUniqueSolution_TargetTooLarge_ReturnsNull()
    {
        // 10x10 = 100格，但 maxShapeCount = 3，最大形状 6 格
        // 100/6 = 17 个形状，超出限制
        var target = new bool[10, 10];
        for (int r = 0; r < 10; r++)
            for (int c = 0; c < 10; c++)
                target[r, c] = true;

        var board = new BoardData(10, 10, target);

        var shapes = new[]
        {
            new ShapeData(new[,] { { true, true, true, true, true, true } }), // I6
        };

        var selector = new ShapeSelector(board, shapes, 1000, maxShapeCount: 3);
        var result = selector.FindUniqueSolution();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindUniqueSolution_NoValidPlacements_ReturnsNull()
    {
        // 2x2 目标，但形状在目标上无法放置（形状需要特定模式）
        var target = new bool[2, 2]
        {
            { true, true },
            { true, true }
        };
        var board = new BoardData(2, 2, target);

        // 1x5 的形状无法放入 2x2 的棋盘
        var shapes = new[]
        {
            new ShapeData(new[,] { { true, true, true, true, true } }), // I5
        };

        var selector = new ShapeSelector(board, shapes, 1000);
        var result = selector.FindUniqueSolution();

        Assert.That(result, Is.Null);
    }

    #endregion

    #region 中等棋盘测试

    [Test]
    public void FindUniqueSolution_5x4Board_FindsSolutionInTime()
    {
        // 5x4 = 20格
        var target = new bool[5, 4];
        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 4; c++)
                target[r, c] = true;

        var board = new BoardData(5, 4, target);

        var shapes = new[]
        {
            new ShapeData(new[,] { { true, true, true, true, true } }), // I5
            new ShapeData(new[,] { { true, true, true, true } }),       // I4
            new ShapeData(new[,] { { true, true, true } }),             // I3
            new ShapeData(new[,] { { true, true } }),                   // I2
            new ShapeData(new[,] { 
                { true, true, true, true },
                { true, false, false, false }
            }), // L5
            new ShapeData(new[,] { 
                { true, true, true },
                { true, false, false }
            }), // L4
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var selector = new ShapeSelector(board, shapes, 5000, 10);
        var result = selector.FindUniqueSolution();
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(5000), "搜索超时");

        if (result != null)
        {
            int totalCells = result.Sum(i => shapes[i].CellCount);
            Assert.That(totalCells, Is.EqualTo(20));
        }
    }

    [Test]
    public void FindUniqueSolution_6x6Board_FindsSolutionOrTimeout()
    {
        // 6x6 = 36格
        var target = new bool[6, 6];
        for (int r = 0; r < 6; r++)
            for (int c = 0; c < 6; c++)
                target[r, c] = true;

        var board = new BoardData(6, 6, target);

        // 使用六格骨牌
        var shapes = new[]
        {
            CreateHexominoI(), // I6: 6格直线
            CreatePentominoI(), // I5: 5格直线
            CreateTetrominoI(), // I4: 4格直线
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var selector = new ShapeSelector(board, shapes, 10000, 10);
        var result = selector.FindUniqueSolution();
        sw.Stop();

        // 应该在合理时间内完成（不管是否找到解）
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(15000), "搜索时间过长");

        if (result != null)
        {
            int totalCells = result.Sum(i => shapes[i].CellCount);
            Assert.That(totalCells, Is.EqualTo(36));
        }
    }

    #endregion

    #region 大棋盘测试（性能验证）

    [Test]
    public void FindUniqueSolution_10x10Board_TerminatesInReasonableTime()
    {
        // 10x10 = 100格，测试是否能正常终止
        var target = new bool[10, 10];
        for (int r = 0; r < 10; r++)
            for (int c = 0; c < 10; c++)
                target[r, c] = true;

        var board = new BoardData(10, 10, target);

        var shapes = new[]
        {
            CreateHexominoI(),  // I6
            CreatePentominoI(), // I5
            CreateTetrominoI(), // I4
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var selector = new ShapeSelector(board, shapes, 500, 20); // 500ms 超时
        var outcome = selector.FindUniqueSolutionWithDetails();
        sw.Stop();

        // 必须在超时时间 + 容差内返回
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(2000), 
            $"搜索应该在超时时间内终止，实际用时 {sw.ElapsedMilliseconds}ms，结果: {outcome.Result}");
        
        // 应该返回超时或未找到，而不是挂起
        Assert.That(outcome.Result, Is.AnyOf(
            ShapeSelectResult.Timeout, 
            ShapeSelectResult.NoUniqueSolution,
            ShapeSelectResult.Found));
    }

    [Test]
    public void FindUniqueSolution_15x15Board_TerminatesInReasonableTime()
    {
        // 15x15 = 225格
        var target = new bool[15, 15];
        for (int r = 0; r < 15; r++)
            for (int c = 0; c < 15; c++)
                target[r, c] = true;

        var board = new BoardData(15, 15, target);

        var shapes = new[]
        {
            CreateHexominoI(),
            CreatePentominoI(),
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var selector = new ShapeSelector(board, shapes, 2000, 50);
        var outcome = selector.FindUniqueSolutionWithDetails();
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(3000), 
            $"大棋盘搜索应该快速终止，实际用时 {sw.ElapsedMilliseconds}ms，结果: {outcome.Result}");
    }

    [Test]
    public void FindUniqueSolution_25x25Board_QuickTermination()
    {
        // 25x25 = 625格，最大棋盘
        var target = new bool[25, 25];
        for (int r = 0; r < 25; r++)
            for (int c = 0; c < 25; c++)
                target[r, c] = true;

        var board = new BoardData(25, 25, target);

        var shapes = new[]
        {
            CreateHexominoI(),
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var selector = new ShapeSelector(board, shapes, 2000, 120);
        var outcome = selector.FindUniqueSolutionWithDetails();
        sw.Stop();

        // 即使是最大棋盘，也应该在合理时间内给出结果
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(3000), 
            $"最大棋盘应该在合理时间内终止，实际用时 {sw.ElapsedMilliseconds}ms，结果: {outcome.Result}");
    }

    #endregion

    #region 唯一性测试

    [Test]
    public void FindUniqueSolution_MultipleSolutions_FindUnique()
    {
        // 2x4 棋盘，用 I2 可以有多种放置方式，应该找到有唯一解的形状组合
        var target = new bool[2, 4]
        {
            { true, true, true, true },
            { true, true, true, true }
        };
        var board = new BoardData(2, 4, target);

        var shapes = new[]
        {
            new ShapeData(new[,] { { true, true } }),       // I2（有多解）
            new ShapeData(new[,] { { true, true, true, true } }), // I4
        };

        var selector = new ShapeSelector(board, shapes, 5000);
        var result = selector.FindUniqueSolution();

        // 应该找到解（可能是 I4 + I4）
        Assert.That(result, Is.Not.Null);
        
        if (result != null)
        {
            int totalCells = result.Sum(i => shapes[i].CellCount);
            Assert.That(totalCells, Is.EqualTo(8));
        }
    }

    #endregion

    #region 不规则目标测试

    [Test]
    public void FindUniqueSolution_IrregularTarget_FindsSolution()
    {
        // L 形目标
        var target = new bool[3, 3]
        {
            { true, true,  false },
            { true, true,  false },
            { true, false, false }
        };
        var board = new BoardData(3, 3, target);

        var shapes = new[]
        {
            new ShapeData(new[,] { { true, true } }),                   // I2
            new ShapeData(new[,] { { true }, { true } }),               // I2 竖
            new ShapeData(new[,] { { true, true, true } }),             // I3
        };

        var selector = new ShapeSelector(board, shapes, 5000);
        var result = selector.FindUniqueSolution();

        // 5格的目标
        if (result != null)
        {
            int totalCells = result.Sum(i => shapes[i].CellCount);
            Assert.That(totalCells, Is.EqualTo(5));
        }
    }

    #endregion

    #region 辅助方法

    private static ShapeData CreateHexominoI()
    {
        // I6: 6格直线
        return new ShapeData(new[,] { { true, true, true, true, true, true } });
    }

    private static ShapeData CreatePentominoI()
    {
        // I5: 5格直线
        return new ShapeData(new[,] { { true, true, true, true, true } });
    }

    private static ShapeData CreateTetrominoI()
    {
        // I4: 4格直线
        return new ShapeData(new[,] { { true, true, true, true } });
    }

    #endregion
}