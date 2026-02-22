namespace PictoMino.Core;

/// <summary>
/// Dancing Links 节点。
/// 每个节点有四个指针（Left, Right, Up, Down）形成双向十字循环链表。
/// </summary>
public class DLXNode
{
    /// <summary>左邻节点</summary>
    public DLXNode Left { get; set; }

    /// <summary>右邻节点</summary>
    public DLXNode Right { get; set; }

    /// <summary>上邻节点</summary>
    public DLXNode Up { get; set; }

    /// <summary>下邻节点</summary>
    public DLXNode Down { get; set; }

    /// <summary>所属列头</summary>
    public DLXColumn? Column { get; set; }

    /// <summary>该节点所属的行索引（用于还原解）</summary>
    public int RowIndex { get; set; } = -1;

    public DLXNode()
    {
        // 初始时指向自身，形成单节点循环
        Left = this;
        Right = this;
        Up = this;
        Down = this;
    }

    /// <summary>
    /// 将本节点插入到 target 节点的右侧。
    /// </summary>
    public void InsertRight(DLXNode target)
    {
        Left = target;
        Right = target.Right;
        target.Right.Left = this;
        target.Right = this;
    }

    /// <summary>
    /// 将本节点插入到 target 节点的下方。
    /// </summary>
    public void InsertDown(DLXNode target)
    {
        Up = target;
        Down = target.Down;
        target.Down.Up = this;
        target.Down = this;
    }

    /// <summary>
    /// 从水平链表中移除本节点（保留自身指针以便恢复）。
    /// </summary>
    public void CoverHorizontal()
    {
        Left.Right = Right;
        Right.Left = Left;
    }

    /// <summary>
    /// 恢复本节点到水平链表中。
    /// </summary>
    public void UncoverHorizontal()
    {
        Left.Right = this;
        Right.Left = this;
    }

    /// <summary>
    /// 从垂直链表中移除本节点（保留自身指针以便恢复）。
    /// </summary>
    public void CoverVertical()
    {
        Up.Down = Down;
        Down.Up = Up;
    }

    /// <summary>
    /// 恢复本节点到垂直链表中。
    /// </summary>
    public void UncoverVertical()
    {
        Up.Down = this;
        Down.Up = this;
    }
}

/// <summary>
/// Dancing Links 列头节点。
/// 除了作为普通节点外，还记录列的元信息。
/// </summary>
public class DLXColumn : DLXNode
{
    /// <summary>列索引</summary>
    public int Index { get; }

    /// <summary>列名称（调试用）</summary>
    public string Name { get; }

    /// <summary>该列当前包含的节点数</summary>
    public int Size { get; set; }

    public DLXColumn(int index, string name = "") : base()
    {
        Index = index;
        Name = name;
        Size = 0;
        Column = this; // 列头的 Column 指向自身
    }

    /// <summary>
    /// 覆盖（Cover）此列：从列头链表中移除，并移除该列所有行。
    /// </summary>
    public void CoverColumn()
    {
        // 从水平列头链表中移除
        CoverHorizontal();

        // 遍历此列的所有节点
        for (var row = Down; row != this; row = row.Down)
        {
            // 移除该行的所有其他节点
            for (var node = row.Right; node != row; node = node.Right)
            {
                node.CoverVertical();
                node.Column!.Size--;
            }
        }
    }

    /// <summary>
    /// 恢复（Uncover）此列：逆序恢复所有被移除的节点。
    /// </summary>
    public void UncoverColumn()
    {
        // 逆序遍历此列的所有节点（从下往上）
        for (var row = Up; row != this; row = row.Up)
        {
            // 恢复该行的所有其他节点（从左往右恢复）
            for (var node = row.Left; node != row; node = node.Left)
            {
                node.Column!.Size++;
                node.UncoverVertical();
            }
        }

        // 恢复列头到水平链表
        UncoverHorizontal();
    }
}
