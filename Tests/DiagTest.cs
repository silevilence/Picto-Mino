using PictoMino.Core;
using System.Diagnostics;
using NUnit.Framework;

namespace PictoMino.Tests;

[TestFixture]
public class DiagTests
{
    [Test]
    [Timeout(5000)]
    public void DiagnoseTimeout()
    {
        var target = new bool[10, 10];
        for (int r = 0; r < 10; r++)
            for (int c = 0; c < 10; c++)
                target[r, c] = true;

        var board = new BoardData(10, 10, target);
        var shapes = new[] {
            new ShapeData(new[,] { { true, true, true, true, true, true } }), // I6
            new ShapeData(new[,] { { true, true, true, true, true } }),       // I5
            new ShapeData(new[,] { { true, true, true, true } }),             // I4
        };

        TestContext.WriteLine("Starting test...");
        var sw = Stopwatch.StartNew();
        var selector = new ShapeSelector(board, shapes, 500, 20);
        TestContext.WriteLine($"Selector created at {sw.ElapsedMilliseconds}ms");
        
        var outcome = selector.FindUniqueSolutionWithDetails();
        sw.Stop();
        
        TestContext.WriteLine($"Result: {outcome.Result}");
        TestContext.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Message: {outcome.Message}");
        TestContext.WriteLine($"SearchCount: {outcome.SearchCount}");
        TestContext.WriteLine($"PruneCount: {outcome.PruneCount}");
        
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(2000), 
            $"应该在超时时间内终止，实际用时 {sw.ElapsedMilliseconds}ms");
    }
}
