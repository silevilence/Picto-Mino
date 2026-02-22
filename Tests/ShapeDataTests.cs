namespace PictoMino.Core.Tests;

[TestFixture]
public class ShapeDataTests
{
    [Test]
    public void Constructor_ClonesMatrix_DoesNotShareReference()
    {
        var matrix = new bool[,] { { true, false }, { false, true } };
        var shape = new ShapeData(matrix);

        // 修改原始矩阵不应影响 ShapeData
        matrix[0, 0] = false;
        Assert.That(shape.Matrix[0, 0], Is.True);
    }

    [Test]
    public void Rows_And_Cols_MatchMatrix()
    {
        var shape = new ShapeData(new bool[,]
        {
            { true, true, false },
            { false, true, true }
        });

        Assert.That(shape.Rows, Is.EqualTo(2));
        Assert.That(shape.Cols, Is.EqualTo(3));
    }

    [Test]
    public void Constructor_NullMatrix_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ShapeData(null!));
    }

    [Test]
    public void CellCount_ReturnsNumberOfTrueCells()
    {
        // L 形：
        // X .
        // X .
        // X X
        var shape = new ShapeData(new bool[,]
        {
            { true, false },
            { true, false },
            { true, true }
        });

        Assert.That(shape.CellCount, Is.EqualTo(4));
    }

    [Test]
    public void CellCount_AllFalse_ReturnsZero()
    {
        var shape = new ShapeData(new bool[,]
        {
            { false, false },
            { false, false }
        });

        Assert.That(shape.CellCount, Is.EqualTo(0));
    }

    [Test]
    public void CellCount_SingleCell_ReturnsOne()
    {
        var shape = new ShapeData(new bool[,] { { true } });
        Assert.That(shape.CellCount, Is.EqualTo(1));
    }

    #region Anchor Tests

    [Test]
    public void Anchor_DefaultsToCenter_OddSize()
    {
        // 3x3 矩阵，锚点应为 (1, 1)
        var shape = new ShapeData(new bool[,]
        {
            { true, true, true },
            { false, true, false },
            { false, true, false }
        });

        Assert.That(shape.AnchorRow, Is.EqualTo(1));
        Assert.That(shape.AnchorCol, Is.EqualTo(1));
    }

    [Test]
    public void Anchor_DefaultsToCenter_EvenSize()
    {
        // 2x4 矩阵，锚点应为 (1, 2)
        var shape = new ShapeData(new bool[,]
        {
            { true, true, true, true },
            { true, false, false, false }
        });

        Assert.That(shape.AnchorRow, Is.EqualTo(1));
        Assert.That(shape.AnchorCol, Is.EqualTo(2));
    }

    [Test]
    public void Anchor_CustomAnchor_Respected()
    {
        var shape = new ShapeData(new bool[,]
        {
            { true, true },
            { true, false }
        }, anchorRow: 0, anchorCol: 0);

        Assert.That(shape.AnchorRow, Is.EqualTo(0));
        Assert.That(shape.AnchorCol, Is.EqualTo(0));
    }

    [Test]
    public void GetCellOffsetsFromAnchor_ReturnsCorrectOffsets()
    {
        // T 形，锚点在中心 (1, 1)：
        // X X X
        // . X .
        var shape = new ShapeData(new bool[,]
        {
            { true, true, true },
            { false, true, false }
        }, anchorRow: 0, anchorCol: 1);

        var offsets = shape.GetCellOffsetsFromAnchor();

        Assert.That(offsets, Has.Count.EqualTo(4));
        Assert.That(offsets, Does.Contain((0, -1)));  // 左上
        Assert.That(offsets, Does.Contain((0, 0)));   // 锚点
        Assert.That(offsets, Does.Contain((0, 1)));   // 右上
        Assert.That(offsets, Does.Contain((1, 0)));   // 下方
    }

    #endregion

    #region Rotation Tests

    [Test]
    public void RotateClockwise_LShape_RotatesCorrectly()
    {
        // L 形：
        // X .
        // X .
        // X X
        var shape = new ShapeData(new bool[,]
        {
            { true, false },
            { true, false },
            { true, true }
        });

        var rotated = shape.RotateClockwise();

        // 顺时针旋转后：
        // X X X
        // X . .
        Assert.That(rotated.Rows, Is.EqualTo(2));
        Assert.That(rotated.Cols, Is.EqualTo(3));
        Assert.That(rotated.Matrix[0, 0], Is.True);
        Assert.That(rotated.Matrix[0, 1], Is.True);
        Assert.That(rotated.Matrix[0, 2], Is.True);
        Assert.That(rotated.Matrix[1, 0], Is.True);
        Assert.That(rotated.Matrix[1, 1], Is.False);
        Assert.That(rotated.Matrix[1, 2], Is.False);
    }

    [Test]
    public void RotateCounterClockwise_LShape_RotatesCorrectly()
    {
        // L 形：
        // X .
        // X .
        // X X
        var shape = new ShapeData(new bool[,]
        {
            { true, false },
            { true, false },
            { true, true }
        });

        var rotated = shape.RotateCounterClockwise();

        // 逆时针旋转后：
        // . . X
        // X X X
        Assert.That(rotated.Rows, Is.EqualTo(2));
        Assert.That(rotated.Cols, Is.EqualTo(3));
        Assert.That(rotated.Matrix[0, 0], Is.False);
        Assert.That(rotated.Matrix[0, 1], Is.False);
        Assert.That(rotated.Matrix[0, 2], Is.True);
        Assert.That(rotated.Matrix[1, 0], Is.True);
        Assert.That(rotated.Matrix[1, 1], Is.True);
        Assert.That(rotated.Matrix[1, 2], Is.True);
    }

    [Test]
    public void RotateClockwise_FourTimes_ReturnsOriginal()
    {
        var original = new ShapeData(new bool[,]
        {
            { true, false },
            { true, true }
        });

        var rotated = original
            .RotateClockwise()
            .RotateClockwise()
            .RotateClockwise()
            .RotateClockwise();

        Assert.That(rotated.Rows, Is.EqualTo(original.Rows));
        Assert.That(rotated.Cols, Is.EqualTo(original.Cols));

        for (int r = 0; r < original.Rows; r++)
            for (int c = 0; c < original.Cols; c++)
                Assert.That(rotated.Matrix[r, c], Is.EqualTo(original.Matrix[r, c]));
    }

    [Test]
    public void RotateClockwise_AnchorRotatesWithShape()
    {
        // 3x2 矩阵，锚点在 (0, 0)
        var shape = new ShapeData(new bool[,]
        {
            { true, true },
            { true, false },
            { true, false }
        }, anchorRow: 0, anchorCol: 0);

        var rotated = shape.RotateClockwise();

        // 顺时针旋转后变为 2x3，锚点从 (0,0) -> (0, 2)
        Assert.That(rotated.AnchorRow, Is.EqualTo(0));
        Assert.That(rotated.AnchorCol, Is.EqualTo(2));
    }

    [Test]
    public void RotateCounterClockwise_AnchorRotatesWithShape()
    {
        // 3x2 矩阵，锚点在 (0, 0)
        var shape = new ShapeData(new bool[,]
        {
            { true, true },
            { true, false },
            { true, false }
        }, anchorRow: 0, anchorCol: 0);

        var rotated = shape.RotateCounterClockwise();

        // 逆时针旋转后变为 2x3，锚点从 (0,0) -> (1, 0)
        Assert.That(rotated.AnchorRow, Is.EqualTo(1));
        Assert.That(rotated.AnchorCol, Is.EqualTo(0));
    }

    [Test]
    public void RotateClockwise_PreservesCellCount()
    {
        var shape = new ShapeData(new bool[,]
        {
            { true, true, true },
            { false, true, false }
        });

        var rotated = shape.RotateClockwise();

        Assert.That(rotated.CellCount, Is.EqualTo(shape.CellCount));
    }

    #endregion
}
