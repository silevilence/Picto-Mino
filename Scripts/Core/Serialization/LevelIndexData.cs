using System.Text.Json.Serialization;

namespace PictoMino.Core.Serialization;

/// <summary>
/// 关卡索引数据（index.json 序列化用）。
/// </summary>
public class LevelIndexData
{
    /// <summary>
    /// 章节列表。
    /// </summary>
    [JsonPropertyName("chapters")]
    public List<ChapterIndexData> Chapters { get; set; } = new();
}

/// <summary>
/// 章节索引数据。
/// </summary>
public class ChapterIndexData
{
    /// <summary>章节唯一标识</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>章节显示名称</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// 关卡文件列表（相对于 Levels 目录的路径）。
    /// 例如: ["tutorial_01.level", "tutorial_02.level"]
    /// </summary>
    [JsonPropertyName("levels")]
    public List<string> Levels { get; set; } = new();
}
