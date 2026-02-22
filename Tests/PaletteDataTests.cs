using PictoMino.Core;

namespace PictoMino.Tests;

[TestFixture]
public class PaletteDataTests
{
    private static ShapeData CreateShape(bool[,] matrix) => new(matrix);

    private static ShapeData[] CreateTestShapes() => new[]
    {
        CreateShape(new bool[,] { { true, true } }),          // 横条
        CreateShape(new bool[,] { { true }, { true } }),       // 竖条
        CreateShape(new bool[,] { { true, true }, { true, false } }) // L形
    };

    [Test]
    public void Constructor_WithShapes_InitializesCorrectly()
    {
        var shapes = CreateTestShapes();
        var palette = new PaletteData(shapes);

        Assert.That(palette.Shapes.Count, Is.EqualTo(3));
        Assert.That(palette.SelectedIndex, Is.EqualTo(-1));
        Assert.That(palette.SelectedShape, Is.Null);
        Assert.That(palette.RemainingCount, Is.EqualTo(3));
    }

    [Test]
    public void Select_ValidIndex_ReturnsTrue()
    {
        var palette = new PaletteData(CreateTestShapes());

        bool result = palette.Select(0);

        Assert.That(result, Is.True);
        Assert.That(palette.SelectedIndex, Is.EqualTo(0));
        Assert.That(palette.SelectedShape, Is.Not.Null);
    }

    [Test]
    public void Select_InvalidIndex_ReturnsFalse()
    {
        var palette = new PaletteData(CreateTestShapes());

        Assert.That(palette.Select(-2), Is.False);
        Assert.That(palette.Select(10), Is.False);
    }

    [Test]
    public void Select_UsedShape_ReturnsFalse()
    {
        var palette = new PaletteData(CreateTestShapes());
        palette.Select(0);
        palette.MarkSelectedAsUsed();

        bool result = palette.Select(0);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Deselect_ClearsSelection()
    {
        var palette = new PaletteData(CreateTestShapes());
        palette.Select(0);

        palette.Deselect();

        Assert.That(palette.SelectedIndex, Is.EqualTo(-1));
        Assert.That(palette.SelectedShape, Is.Null);
    }

    [Test]
    public void MarkSelectedAsUsed_UpdatesStateCorrectly()
    {
        var palette = new PaletteData(CreateTestShapes());
        palette.Select(1);

        bool result = palette.MarkSelectedAsUsed();

        Assert.That(result, Is.True);
        Assert.That(palette.IsUsed(1), Is.True);
        Assert.That(palette.SelectedIndex, Is.EqualTo(-1));
        Assert.That(palette.RemainingCount, Is.EqualTo(2));
    }

    [Test]
    public void MarkSelectedAsUsed_NoSelection_ReturnsFalse()
    {
        var palette = new PaletteData(CreateTestShapes());

        bool result = palette.MarkSelectedAsUsed();

        Assert.That(result, Is.False);
    }

    [Test]
    public void MarkAsUnused_RestoresShape()
    {
        var palette = new PaletteData(CreateTestShapes());
        palette.Select(0);
        palette.MarkSelectedAsUsed();

        bool result = palette.MarkAsUnused(0);

        Assert.That(result, Is.True);
        Assert.That(palette.IsUsed(0), Is.False);
        Assert.That(palette.RemainingCount, Is.EqualTo(3));
    }

    [Test]
    public void Reset_ClearsAllUsedStates()
    {
        var palette = new PaletteData(CreateTestShapes());
        palette.Select(0);
        palette.MarkSelectedAsUsed();
        palette.Select(1);
        palette.MarkSelectedAsUsed();

        palette.Reset();

        Assert.That(palette.RemainingCount, Is.EqualTo(3));
        Assert.That(palette.SelectedIndex, Is.EqualTo(-1));
    }

    [Test]
    public void SelectNext_CyclesThroughAvailable()
    {
        var palette = new PaletteData(CreateTestShapes());

        Assert.That(palette.SelectNext(), Is.True);
        Assert.That(palette.SelectedIndex, Is.EqualTo(0));

        Assert.That(palette.SelectNext(), Is.True);
        Assert.That(palette.SelectedIndex, Is.EqualTo(1));

        Assert.That(palette.SelectNext(), Is.True);
        Assert.That(palette.SelectedIndex, Is.EqualTo(2));

        Assert.That(palette.SelectNext(), Is.True);
        Assert.That(palette.SelectedIndex, Is.EqualTo(0)); // 循环
    }

    [Test]
    public void SelectNext_SkipsUsedShapes()
    {
        var palette = new PaletteData(CreateTestShapes());
        palette.Select(1);
        palette.MarkSelectedAsUsed(); // 标记索引1为已使用

        palette.Select(0);
        bool result = palette.SelectNext(); // 应跳过1，选中2

        Assert.That(result, Is.True);
        Assert.That(palette.SelectedIndex, Is.EqualTo(2));
    }

    [Test]
    public void SelectPrevious_CyclesBackward()
    {
        var palette = new PaletteData(CreateTestShapes());
        palette.Select(0);

        Assert.That(palette.SelectPrevious(), Is.True);
        Assert.That(palette.SelectedIndex, Is.EqualTo(2)); // 循环到末尾
    }

    [Test]
    public void OnSelectionChanged_FiresOnSelect()
    {
        var palette = new PaletteData(CreateTestShapes());
        int oldIdx = -999, newIdx = -999;
        palette.OnSelectionChanged += (o, n) => { oldIdx = o; newIdx = n; };

        palette.Select(1);

        Assert.That(oldIdx, Is.EqualTo(-1));
        Assert.That(newIdx, Is.EqualTo(1));
    }

    [Test]
    public void OnShapeUsedChanged_FiresOnMarkUsed()
    {
        var palette = new PaletteData(CreateTestShapes());
        int firedIndex = -1;
        bool firedUsed = false;
        palette.OnShapeUsedChanged += (i, u) => { firedIndex = i; firedUsed = u; };

        palette.Select(0);
        palette.MarkSelectedAsUsed();

        Assert.That(firedIndex, Is.EqualTo(0));
        Assert.That(firedUsed, Is.True);
    }

    [Test]
    public void OnAllShapesUsed_FiresWhenAllUsed()
    {
        var shapes = new[] { CreateShape(new bool[,] { { true } }) };
        var palette = new PaletteData(shapes);
        bool fired = false;
        palette.OnAllShapesUsed += () => fired = true;

        palette.Select(0);
        palette.MarkSelectedAsUsed();

        Assert.That(fired, Is.True);
    }
}
