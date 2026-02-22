using PictoMino.Core;
using PictoMino.Core.Serialization;

namespace PictoMino.Tests.Serialization;

[TestFixture]
public class ShapeFileDataTests
{
    [Test]
    public void FromShapeData_ConvertsCorrectly()
    {
        var shape = new ShapeData(new bool[,]
        {
            { true, true },
            { true, false },
            { true, false }
        });

        var fileData = ShapeFileData.FromShapeData(shape, "L", "L形");

        Assert.That(fileData.Id, Is.EqualTo("L"));
        Assert.That(fileData.Name, Is.EqualTo("L形"));
        Assert.That(fileData.Matrix.Length, Is.EqualTo(3));
        Assert.That(fileData.Matrix[0], Is.EqualTo("##"));
        Assert.That(fileData.Matrix[1], Is.EqualTo("#."));
        Assert.That(fileData.Matrix[2], Is.EqualTo("#."));
    }

    [Test]
    public void ToShapeData_ConvertsCorrectly()
    {
        var fileData = new ShapeFileData
        {
            Id = "T",
            Name = "T形",
            Matrix = new[] { "###", ".#." },
            AnchorRow = 0,
            AnchorCol = 1
        };

        var shape = fileData.ToShapeData();

        Assert.That(shape.Rows, Is.EqualTo(2));
        Assert.That(shape.Cols, Is.EqualTo(3));
        Assert.That(shape.Matrix[0, 0], Is.True);
        Assert.That(shape.Matrix[0, 1], Is.True);
        Assert.That(shape.Matrix[0, 2], Is.True);
        Assert.That(shape.Matrix[1, 0], Is.False);
        Assert.That(shape.Matrix[1, 1], Is.True);
        Assert.That(shape.Matrix[1, 2], Is.False);
        Assert.That(shape.AnchorRow, Is.EqualTo(0));
        Assert.That(shape.AnchorCol, Is.EqualTo(1));
    }

    [Test]
    public void RoundTrip_PreservesData()
    {
        var original = new ShapeData(new bool[,]
        {
            { true, true, true, true }
        }, 0, 1);

        var fileData = ShapeFileData.FromShapeData(original);
        var restored = fileData.ToShapeData();

        Assert.That(restored.Rows, Is.EqualTo(original.Rows));
        Assert.That(restored.Cols, Is.EqualTo(original.Cols));
        Assert.That(restored.CellCount, Is.EqualTo(original.CellCount));
        Assert.That(restored.AnchorRow, Is.EqualTo(original.AnchorRow));
        Assert.That(restored.AnchorCol, Is.EqualTo(original.AnchorCol));
    }

    [Test]
    public void ToShapeData_EmptyMatrix_Throws()
    {
        var fileData = new ShapeFileData { Matrix = Array.Empty<string>() };

        Assert.Throws<InvalidOperationException>(() => fileData.ToShapeData());
    }
}
