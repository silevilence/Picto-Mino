namespace PictoMino.Core.Tests;

/// <summary>
/// Dancing Links 数据结构单元测试。
/// </summary>
[TestFixture]
public class DLXNodeTests
{
    // ─── Node 基础结构 ───────────────────────────────────

    [Test]
    public void NewNode_PointsToItself()
    {
        var node = new DLXNode();

        Assert.That(node.Left, Is.EqualTo(node));
        Assert.That(node.Right, Is.EqualTo(node));
        Assert.That(node.Up, Is.EqualTo(node));
        Assert.That(node.Down, Is.EqualTo(node));
    }

    [Test]
    public void Node_CanLinkHorizontally()
    {
        var a = new DLXNode();
        var b = new DLXNode();

        // 将 b 插入到 a 的右侧
        b.InsertRight(a);

        Assert.That(a.Right, Is.EqualTo(b));
        Assert.That(b.Left, Is.EqualTo(a));
        Assert.That(b.Right, Is.EqualTo(a)); // 循环
        Assert.That(a.Left, Is.EqualTo(b));
    }

    [Test]
    public void Node_CanLinkVertically()
    {
        var a = new DLXNode();
        var b = new DLXNode();

        // 将 b 插入到 a 的下方
        b.InsertDown(a);

        Assert.That(a.Down, Is.EqualTo(b));
        Assert.That(b.Up, Is.EqualTo(a));
        Assert.That(b.Down, Is.EqualTo(a)); // 循环
        Assert.That(a.Up, Is.EqualTo(b));
    }

    [Test]
    public void Node_CoverRemovesFromHorizontalList()
    {
        var a = new DLXNode();
        var b = new DLXNode();
        var c = new DLXNode();

        b.InsertRight(a);
        c.InsertRight(b);
        // 链: a <-> b <-> c <-> a

        b.CoverHorizontal();

        Assert.That(a.Right, Is.EqualTo(c));
        Assert.That(c.Left, Is.EqualTo(a));
        // b 仍保持原指针（用于 uncover）
        Assert.That(b.Left, Is.EqualTo(a));
        Assert.That(b.Right, Is.EqualTo(c));
    }

    [Test]
    public void Node_UncoverRestoresToHorizontalList()
    {
        var a = new DLXNode();
        var b = new DLXNode();
        var c = new DLXNode();

        b.InsertRight(a);
        c.InsertRight(b);

        b.CoverHorizontal();
        b.UncoverHorizontal();

        Assert.That(a.Right, Is.EqualTo(b));
        Assert.That(b.Right, Is.EqualTo(c));
        Assert.That(c.Left, Is.EqualTo(b));
        Assert.That(b.Left, Is.EqualTo(a));
    }

    [Test]
    public void Node_CoverRemovesFromVerticalList()
    {
        var a = new DLXNode();
        var b = new DLXNode();
        var c = new DLXNode();

        b.InsertDown(a);
        c.InsertDown(b);
        // 链: a <-> b <-> c <-> a (垂直)

        b.CoverVertical();

        Assert.That(a.Down, Is.EqualTo(c));
        Assert.That(c.Up, Is.EqualTo(a));
    }

    [Test]
    public void Node_UncoverRestoresToVerticalList()
    {
        var a = new DLXNode();
        var b = new DLXNode();
        var c = new DLXNode();

        b.InsertDown(a);
        c.InsertDown(b);

        b.CoverVertical();
        b.UncoverVertical();

        Assert.That(a.Down, Is.EqualTo(b));
        Assert.That(b.Down, Is.EqualTo(c));
    }

    // ─── Column 结构 ────────────────────────────────────

    [Test]
    public void NewColumn_HasZeroSize()
    {
        var column = new DLXColumn(0, "test");

        Assert.That(column.Size, Is.EqualTo(0));
        Assert.That(column.Name, Is.EqualTo("test"));
        Assert.That(column.Index, Is.EqualTo(0));
    }

    [Test]
    public void Column_AddNode_IncrementsSize()
    {
        var column = new DLXColumn(0, "col");
        var node = new DLXNode { Column = column };

        node.InsertDown(column);
        column.Size++;

        Assert.That(column.Size, Is.EqualTo(1));
        Assert.That(column.Down, Is.EqualTo(node));
    }

    [Test]
    public void Column_CoverColumn_RemovesFromHeader()
    {
        var header = new DLXNode();
        var col1 = new DLXColumn(0, "c1");
        var col2 = new DLXColumn(1, "c2");
        var col3 = new DLXColumn(2, "c3");

        col1.InsertRight(header);
        col2.InsertRight(col1);
        col3.InsertRight(col2);

        col2.CoverColumn();

        Assert.That(col1.Right, Is.EqualTo(col3));
        Assert.That(col3.Left, Is.EqualTo(col1));
    }

    [Test]
    public void Column_UncoverColumn_RestoresToHeader()
    {
        var header = new DLXNode();
        var col1 = new DLXColumn(0, "c1");
        var col2 = new DLXColumn(1, "c2");
        var col3 = new DLXColumn(2, "c3");

        col1.InsertRight(header);
        col2.InsertRight(col1);
        col3.InsertRight(col2);

        col2.CoverColumn();
        col2.UncoverColumn();

        Assert.That(col1.Right, Is.EqualTo(col2));
        Assert.That(col2.Right, Is.EqualTo(col3));
    }
}
