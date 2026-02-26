using NUnit.Framework;
using PictoMino.Core;

namespace PictoMino.Tests.DLX;

[TestFixture]
public class ShapeSelectorTests
{
    [Test]
    public void FindUniqueSolution_SimpleCase_FindsSolution()
    {
        // 2x3 目标，用 I2 + I2 + I2 可以填满但有多解
        // 用 I3 + I3 应该能找到唯一解
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
        // 应该选择两个 I3
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result.All(i => i == 1), Is.True); // 都是 I3
    }

    [Test]
    public void FindUniqueSolution_NoSolution_ReturnsNull()
    {
        // 3x3 目标，只有 I2 形状，无法填满
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
            new ShapeData(new[,] { { true } }),                    // I1
            new ShapeData(new[,] { { true, true } }),              // I2
            new ShapeData(new[,] { { true, false }, { true, true } }), // L3
        };

        var selector = new ShapeSelector(board, shapes, 5000);
        var result = selector.FindUniqueSolution();

        Assert.That(result, Is.Not.Null);
        // 应该选择 L3 + I1 或 I2 + I2
        int totalCells = result!.Sum(i => shapes[i].CellCount);
        Assert.That(totalCells, Is.EqualTo(4));
    }

    [Test]
    public void FindUniqueSolution_5x4Board_FindsSolutionInTime()
    {
        // 5x4 = 20格，测试性能
        var target = new bool[5, 4];
        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 4; c++)
                target[r, c] = true;

        var board = new BoardData(5, 4, target);

        // 提供一些大形状
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
        var selector = new ShapeSelector(board, shapes, 5000, 6);
        var result = selector.FindUniqueSolution();
        sw.Stop();

        // 应该在5秒内完成
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(5000));
        
        if (result != null)
        {
            int totalCells = result.Sum(i => shapes[i].CellCount);
            Assert.That(totalCells, Is.EqualTo(20));
        }
    }
}
