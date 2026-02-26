namespace PictoMino.Core.Tests;

/// <summary>
/// 棋盘状态到 DLX 矩阵转换测试。
/// </summary>
[TestFixture]
public class BoardToDLXConverterTests
{
    // ─── 基础转换 ────────────────────────────────────────

    [Test]
    public void Convert_SingleShapeSingleCell_CorrectMatrix()
    {
        // 1x1 棋盘，1x1 形状
        var board = new BoardData(1, 1);
        var shapes = new[] { new ShapeData(new bool[,] { { true } }) };

        var converter = new BoardToDLXConverter(board, shapes);
        var matrix = converter.BuildMatrix();

        // 只有一种放置方式：形状0放在(0,0)
        // 列: [cell_0_0, shape_0]
        Assert.That(matrix.GetLength(0), Is.EqualTo(1)); // 1 行
        Assert.That(matrix.GetLength(1), Is.EqualTo(2)); // 2 列 (1 cell + 1 shape)
        Assert.That(matrix[0, 0], Is.EqualTo(1)); // 覆盖格子
        Assert.That(matrix[0, 1], Is.EqualTo(1)); // 使用形状
    }

    [Test]
    public void Convert_ShapeFitsMultiplePlaces_AllPlacements()
    {
        // 2x2 棋盘，1x1 形状
        // 形状可以放在 4 个位置
        var board = new BoardData(2, 2);
        var shapes = new[] { new ShapeData(new bool[,] { { true } }) };

        var converter = new BoardToDLXConverter(board, shapes);
        var matrix = converter.BuildMatrix();

        // 4 种放置方式
        // 列: [cell_0_0, cell_0_1, cell_1_0, cell_1_1, shape_0]
        Assert.That(matrix.GetLength(0), Is.EqualTo(4)); // 4 行
        Assert.That(matrix.GetLength(1), Is.EqualTo(5)); // 5 列 (4 cells + 1 shape)
    }

    [Test]
    public void Convert_LargerShape_FewerPlacements()
    {
        // 2x2 棋盘，横条形状 (1x2)
        // 形状可以放在 4 个位置: 横向 (0,0), (1,0) + 竖向旋转 (0,0), (0,1)
        var board = new BoardData(2, 2);
        var shapes = new[] { new ShapeData(new bool[,] { { true, true } }) };

        var converter = new BoardToDLXConverter(board, shapes);
        var matrix = converter.BuildMatrix();

        Assert.That(matrix.GetLength(0), Is.EqualTo(4)); // 4 种放置（含旋转）
    }

    [Test]
    public void Convert_MultipleShapes_AllCombinations()
    {
        // 2x2 棋盘，两个 1x1 形状
        var board = new BoardData(2, 2);
        var shapes = new[]
        {
            new ShapeData(new bool[,] { { true } }),
            new ShapeData(new bool[,] { { true } })
        };

        var converter = new BoardToDLXConverter(board, shapes);
        var matrix = converter.BuildMatrix();

        // 每个形状各有 4 种放置，共 8 种
        // 列: [cell_0_0, cell_0_1, cell_1_0, cell_1_1, shape_0, shape_1]
        Assert.That(matrix.GetLength(0), Is.EqualTo(8)); // 8 行
        Assert.That(matrix.GetLength(1), Is.EqualTo(6)); // 6 列
    }

    [Test]
    public void Convert_ShapeDoesNotFit_NoPlacements()
    {
        // 2x2 棋盘，3x1 形状（放不下）
        var board = new BoardData(2, 2);
        var shapes = new[] { new ShapeData(new bool[,] { { true, true, true } }) };

        var converter = new BoardToDLXConverter(board, shapes);
        var matrix = converter.BuildMatrix();

        Assert.That(matrix.GetLength(0), Is.EqualTo(0)); // 无可用放置
    }

    // ─── 目标图案支持 ───────────────────────────────────

    [Test]
    public void Convert_WithTarget_OnlyTargetCellsAsColumns()
    {
        // 2x2 棋盘，只有对角线是目标
        // Target:
        // X .
        // . X
        var target = new bool[,]
        {
            { true, false },
            { false, true }
        };
        var board = new BoardData(2, 2, target);
        var shapes = new[] { new ShapeData(new bool[,] { { true } }) };

        var converter = new BoardToDLXConverter(board, shapes);
        var matrix = converter.BuildMatrix();

        // 只有 2 个目标格子需要覆盖
        // 1x1 形状可以放在 2 个目标位置
        // 列: [cell_target_0, cell_target_1, shape_0]
        Assert.That(matrix.GetLength(0), Is.EqualTo(2)); // 2 种放置
        Assert.That(matrix.GetLength(1), Is.EqualTo(3)); // 3 列 (2 target cells + 1 shape)
    }

    [Test]
    public void Convert_ShapeOverlapsNonTarget_Excluded()
    {
        // 2x2 棋盘，只有 (0,0) 是目标
        // 横条形状会覆盖 (0,0) 和 (0,1)，但 (0,1) 不是目标
        var target = new bool[,]
        {
            { true, false },
            { false, false }
        };
        var board = new BoardData(2, 2, target);
        var shapes = new[] { new ShapeData(new bool[,] { { true, true } }) };

        var converter = new BoardToDLXConverter(board, shapes);
        var matrix = converter.BuildMatrix();

        // 横条形状无法只覆盖目标格子，所以没有有效放置
        Assert.That(matrix.GetLength(0), Is.EqualTo(0));
    }

    // ─── 放置信息导出 ───────────────────────────────────

    [Test]
    public void GetPlacementInfo_ReturnsCorrectData()
    {
        var board = new BoardData(2, 2);
        var shapes = new[] { new ShapeData(new bool[,] { { true, true } }) };

        var converter = new BoardToDLXConverter(board, shapes);
        converter.BuildMatrix();

        var placements = converter.GetPlacements();

        // 横向 2 个位置 + 竖向旋转 2 个位置 = 4 种放置
        Assert.That(placements, Has.Count.EqualTo(4));
        // 所有放置都是形状 0
        Assert.That(placements.All(p => p.ShapeIndex == 0), Is.True);
    }
}
