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

    public ShapeData(bool[,] matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        Matrix = (bool[,])matrix.Clone();

        int count = 0;
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (Matrix[r, c]) count++;
        CellCount = count;
    }
}
