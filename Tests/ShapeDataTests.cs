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
}
