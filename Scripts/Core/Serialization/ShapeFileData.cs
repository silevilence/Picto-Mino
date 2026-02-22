using System.Text.Json.Serialization;

namespace PictoMino.Core.Serialization;

/// <summary>
/// 形状文件数据（JSON 序列化用）。
/// </summary>
public class ShapeFileData
{
    /// <summary>形状唯一标识</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>形状显示名称</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// 形状矩阵，用字符串数组表示。
    /// 每行一个字符串，'#' 表示填充，'.' 表示空。
    /// 例如: ["##", "#.", "#."] 表示 L 形。
    /// </summary>
    [JsonPropertyName("matrix")]
    public string[] Matrix { get; set; } = Array.Empty<string>();

    /// <summary>锚点行坐标，-1 表示自动居中</summary>
    [JsonPropertyName("anchorRow")]
    public int AnchorRow { get; set; } = -1;

    /// <summary>锚点列坐标，-1 表示自动居中</summary>
    [JsonPropertyName("anchorCol")]
    public int AnchorCol { get; set; } = -1;

    /// <summary>
    /// 从 ShapeData 创建文件数据。
    /// </summary>
    public static ShapeFileData FromShapeData(ShapeData shape, string id = "", string name = "")
    {
        var rows = shape.Rows;
        var cols = shape.Cols;
        var matrix = new string[rows];

        for (int r = 0; r < rows; r++)
        {
            var chars = new char[cols];
            for (int c = 0; c < cols; c++)
            {
                chars[c] = shape.Matrix[r, c] ? '#' : '.';
            }
            matrix[r] = new string(chars);
        }

        return new ShapeFileData
        {
            Id = id,
            Name = name,
            Matrix = matrix,
            AnchorRow = shape.AnchorRow,
            AnchorCol = shape.AnchorCol
        };
    }

    /// <summary>
    /// 转换为 ShapeData。
    /// </summary>
    public ShapeData ToShapeData()
    {
        if (Matrix.Length == 0)
            throw new InvalidOperationException("Matrix is empty.");

        int rows = Matrix.Length;
        int cols = Matrix.Max(row => row.Length);
        var boolMatrix = new bool[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < Matrix[r].Length; c++)
            {
                boolMatrix[r, c] = Matrix[r][c] == '#';
            }
        }

        return new ShapeData(boolMatrix, AnchorRow, AnchorCol);
    }
}
