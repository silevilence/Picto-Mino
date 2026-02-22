namespace PictoMino.Core;

/// <summary>
/// 定义多格骨牌的形状数据。
/// 使用布尔矩阵表示形状，true 为填充格。
/// </summary>
public class ShapeData
{
    /// <summary>
    /// 形状矩阵（行优先：[row, col]）。true 表示该格被占据。
    /// </summary>
    public bool[,] Matrix { get; }

    /// <summary>矩阵行数</summary>
    public int Rows => Matrix.GetLength(0);

    /// <summary>矩阵列数</summary>
    public int Cols => Matrix.GetLength(1);

    /// <summary>被占据的格子总数。</summary>
    public int CellCount { get; }

    /// <summary>锚点行坐标（放置和旋转的中心点）</summary>
    public int AnchorRow { get; }

    /// <summary>锚点列坐标（放置和旋转的中心点）</summary>
    public int AnchorCol { get; }

    /// <summary>
    /// 创建形状数据，锚点默认为中心格。
    /// </summary>
    public ShapeData(bool[,] matrix) : this(matrix, -1, -1)
    {
    }

    /// <summary>
    /// 创建形状数据，指定锚点位置。
    /// </summary>
    /// <param name="matrix">形状矩阵</param>
    /// <param name="anchorRow">锚点行坐标，-1 表示自动居中</param>
    /// <param name="anchorCol">锚点列坐标，-1 表示自动居中</param>
    public ShapeData(bool[,] matrix, int anchorRow, int anchorCol)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        Matrix = (bool[,])matrix.Clone();

        int count = 0;
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (Matrix[r, c]) count++;
        CellCount = count;

        // 自动计算锚点（居中）
        AnchorRow = anchorRow >= 0 ? anchorRow : Rows / 2;
        AnchorCol = anchorCol >= 0 ? anchorCol : Cols / 2;
    }

    /// <summary>
    /// 顺时针旋转 90 度，返回新的 ShapeData。
    /// </summary>
    public ShapeData RotateClockwise()
    {
        int newRows = Cols;
        int newCols = Rows;
        var newMatrix = new bool[newRows, newCols];

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                // 顺时针旋转：(r, c) -> (c, Rows - 1 - r)
                newMatrix[c, Rows - 1 - r] = Matrix[r, c];
            }
        }

        // 锚点也要跟着旋转：(anchorRow, anchorCol) -> (anchorCol, Rows - 1 - anchorRow)
        int newAnchorRow = AnchorCol;
        int newAnchorCol = Rows - 1 - AnchorRow;

        return new ShapeData(newMatrix, newAnchorRow, newAnchorCol);
    }

    /// <summary>
    /// 逆时针旋转 90 度，返回新的 ShapeData。
    /// </summary>
    public ShapeData RotateCounterClockwise()
    {
        int newRows = Cols;
        int newCols = Rows;
        var newMatrix = new bool[newRows, newCols];

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                // 逆时针旋转：(r, c) -> (Cols - 1 - c, r)
                newMatrix[Cols - 1 - c, r] = Matrix[r, c];
            }
        }

        // 锚点也要跟着旋转：(anchorRow, anchorCol) -> (Cols - 1 - anchorCol, anchorRow)
        int newAnchorRow = Cols - 1 - AnchorCol;
        int newAnchorCol = AnchorRow;

        return new ShapeData(newMatrix, newAnchorRow, newAnchorCol);
    }

    /// <summary>
    /// 获取形状相对于锚点的所有填充格偏移量。
    /// </summary>
    /// <returns>偏移量列表 (deltaRow, deltaCol)</returns>
    public List<(int DeltaRow, int DeltaCol)> GetCellOffsetsFromAnchor()
    {
        var offsets = new List<(int, int)>();
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                if (Matrix[r, c])
                {
                    offsets.Add((r - AnchorRow, c - AnchorCol));
                }
            }
        }
        return offsets;
    }
}
