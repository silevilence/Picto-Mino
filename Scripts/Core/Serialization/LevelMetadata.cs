using System.Text.Json.Serialization;

namespace PictoMino.Core.Serialization;

/// <summary>
/// 关卡元数据（metadata.json 序列化用）。
/// </summary>
public class LevelMetadata
{
    /// <summary>当前格式版本</summary>
    public const int CurrentVersion = 1;

    /// <summary>格式版本号</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// 形状索引。键为形状 ID，值为形状来源。
    /// 来源格式：
    /// - "builtin:xxx" 表示内置形状
    /// - "custom:filename.shape.json" 表示自定义形状文件
    /// </summary>
    [JsonPropertyName("shapeIndex")]
    public Dictionary<string, string> ShapeIndex { get; set; } = new();

    /// <summary>
    /// 颜色索引。键为形状 ID，值为颜色十六进制值（如 "#FF5733"）。
    /// </summary>
    [JsonPropertyName("colorIndex")]
    public Dictionary<string, string> ColorIndex { get; set; } = new();

    /// <summary>
    /// 作者信息。
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>
    /// 关卡描述。
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 创建时间（ISO 8601 格式）。
    /// </summary>
    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// 添加内置形状引用。
    /// </summary>
    public void AddBuiltinShape(string shapeId, string builtinName, string? color = null)
    {
        ShapeIndex[shapeId] = $"builtin:{builtinName}";
        if (color != null)
            ColorIndex[shapeId] = color;
    }

    /// <summary>
    /// 添加自定义形状引用。
    /// </summary>
    public void AddCustomShape(string shapeId, string filename, string? color = null)
    {
        ShapeIndex[shapeId] = $"custom:{filename}";
        if (color != null)
            ColorIndex[shapeId] = color;
    }

    /// <summary>
    /// 解析形状来源。
    /// </summary>
    /// <returns>(isBuiltin, name/filename)</returns>
    public static (bool IsBuiltin, string Name) ParseShapeSource(string source)
    {
        if (source.StartsWith("builtin:"))
            return (true, source[8..]);
        if (source.StartsWith("custom:"))
            return (false, source[7..]);
        throw new FormatException($"Invalid shape source format: {source}");
    }
}
