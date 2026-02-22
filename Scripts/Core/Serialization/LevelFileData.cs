using System.Text.Json.Serialization;

namespace PictoMino.Core.Serialization;

/// <summary>
/// 关卡文件数据（level.json 序列化用）。
/// </summary>
public class LevelFileData
{
    /// <summary>关卡唯一标识</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>关卡显示名称</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>关卡难度 (1-5)</summary>
    [JsonPropertyName("difficulty")]
    public int Difficulty { get; set; } = 1;

    /// <summary>棋盘行数</summary>
    [JsonPropertyName("rows")]
    public int Rows { get; set; }

    /// <summary>棋盘列数</summary>
    [JsonPropertyName("cols")]
    public int Cols { get; set; }

    /// <summary>
    /// 目标图案，用字符串数组表示。
    /// '#' 表示应填充，'.' 表示应为空。
    /// 为 null 时表示需要全部填满。
    /// </summary>
    [JsonPropertyName("target")]
    public string[]? Target { get; set; }

    /// <summary>
    /// 使用的形状 ID 列表。
    /// 引用 metadata.json 中的 shapeIndex 或内置形状。
    /// </summary>
    [JsonPropertyName("shapeIds")]
    public string[] ShapeIds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 从 LevelData 创建文件数据。
    /// </summary>
    /// <param name="level">关卡数据</param>
    /// <param name="shapeIds">形状 ID 列表（与 Shapes 一一对应）</param>
    public static LevelFileData FromLevelData(LevelData level, string[] shapeIds)
    {
        string[]? target = null;
        if (level.Target != null)
        {
            target = new string[level.Rows];
            for (int r = 0; r < level.Rows; r++)
            {
                var chars = new char[level.Cols];
                for (int c = 0; c < level.Cols; c++)
                {
                    chars[c] = level.Target[r, c] ? '#' : '.';
                }
                target[r] = new string(chars);
            }
        }

        return new LevelFileData
        {
            Id = level.Id,
            Name = level.Name,
            Difficulty = level.Difficulty,
            Rows = level.Rows,
            Cols = level.Cols,
            Target = target,
            ShapeIds = shapeIds
        };
    }

    /// <summary>
    /// 解析目标图案为 bool 数组。
    /// </summary>
    public bool[,]? ParseTarget()
    {
        if (Target == null || Target.Length == 0)
            return null;

        var result = new bool[Rows, Cols];
        for (int r = 0; r < Math.Min(Target.Length, Rows); r++)
        {
            for (int c = 0; c < Math.Min(Target[r].Length, Cols); c++)
            {
                result[r, c] = Target[r][c] == '#';
            }
        }
        return result;
    }
}
