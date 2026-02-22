using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PictoMino.Core.Serialization;

/// <summary>
/// 关卡包。处理 .level 文件的读写（zip 格式）。
/// </summary>
public class LevelPackage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>关卡数据</summary>
    public LevelFileData Level { get; set; } = new();

    /// <summary>元数据</summary>
    public LevelMetadata Metadata { get; set; } = new();

    /// <summary>自定义形状（键为文件名）</summary>
    public Dictionary<string, ShapeFileData> CustomShapes { get; set; } = new();

    /// <summary>
    /// 从字节数组加载关卡包。
    /// </summary>
    public static LevelPackage Load(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return Load(stream);
    }

    /// <summary>
    /// 从流加载关卡包。
    /// </summary>
    public static LevelPackage Load(Stream stream)
    {
        var package = new LevelPackage();

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        // 读取 metadata.json
        var metadataEntry = archive.GetEntry("metadata.json");
        if (metadataEntry != null)
        {
            using var reader = new StreamReader(metadataEntry.Open());
            var json = reader.ReadToEnd();
            package.Metadata = JsonSerializer.Deserialize<LevelMetadata>(json, JsonOptions) ?? new();
        }

        // 读取 level.json
        var levelEntry = archive.GetEntry("level.json");
        if (levelEntry != null)
        {
            using var reader = new StreamReader(levelEntry.Open());
            var json = reader.ReadToEnd();
            package.Level = JsonSerializer.Deserialize<LevelFileData>(json, JsonOptions) ?? new();
        }

        // 读取所有 .shape.json 文件
        foreach (var entry in archive.Entries)
        {
            if (entry.Name.EndsWith(".shape.json", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(entry.Open());
                var json = reader.ReadToEnd();
                var shape = JsonSerializer.Deserialize<ShapeFileData>(json, JsonOptions);
                if (shape != null)
                {
                    package.CustomShapes[entry.Name] = shape;
                }
            }
        }

        return package;
    }

    /// <summary>
    /// 从文件加载关卡包。
    /// </summary>
    public static LevelPackage LoadFromFile(string path)
    {
        var data = File.ReadAllBytes(path);
        return Load(data);
    }

    /// <summary>
    /// 保存为字节数组。
    /// </summary>
    public byte[] Save()
    {
        using var stream = new MemoryStream();
        Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// 保存到流。
    /// </summary>
    public void Save(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        // 写入 metadata.json
        var metadataEntry = archive.CreateEntry("metadata.json");
        using (var writer = new StreamWriter(metadataEntry.Open()))
        {
            var json = JsonSerializer.Serialize(Metadata, JsonOptions);
            writer.Write(json);
        }

        // 写入 level.json
        var levelEntry = archive.CreateEntry("level.json");
        using (var writer = new StreamWriter(levelEntry.Open()))
        {
            var json = JsonSerializer.Serialize(Level, JsonOptions);
            writer.Write(json);
        }

        // 写入自定义形状
        foreach (var (filename, shape) in CustomShapes)
        {
            var shapeEntry = archive.CreateEntry(filename);
            using var writer = new StreamWriter(shapeEntry.Open());
            var json = JsonSerializer.Serialize(shape, JsonOptions);
            writer.Write(json);
        }
    }

    /// <summary>
    /// 保存到文件。
    /// </summary>
    public void SaveToFile(string path)
    {
        var data = Save();
        File.WriteAllBytes(path, data);
    }

    /// <summary>
    /// 导出为目录（不打包）。
    /// </summary>
    public void ExportToDirectory(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);

        // 写入 metadata.json
        var metadataPath = Path.Combine(directoryPath, "metadata.json");
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(Metadata, JsonOptions));

        // 写入 level.json
        var levelPath = Path.Combine(directoryPath, "level.json");
        File.WriteAllText(levelPath, JsonSerializer.Serialize(Level, JsonOptions));

        // 写入自定义形状
        foreach (var (filename, shape) in CustomShapes)
        {
            var shapePath = Path.Combine(directoryPath, filename);
            File.WriteAllText(shapePath, JsonSerializer.Serialize(shape, JsonOptions));
        }
    }

    /// <summary>
    /// 从目录导入。
    /// </summary>
    public static LevelPackage ImportFromDirectory(string directoryPath)
    {
        var package = new LevelPackage();

        // 读取 metadata.json
        var metadataPath = Path.Combine(directoryPath, "metadata.json");
        if (File.Exists(metadataPath))
        {
            var json = File.ReadAllText(metadataPath);
            package.Metadata = JsonSerializer.Deserialize<LevelMetadata>(json, JsonOptions) ?? new();
        }

        // 读取 level.json
        var levelPath = Path.Combine(directoryPath, "level.json");
        if (File.Exists(levelPath))
        {
            var json = File.ReadAllText(levelPath);
            package.Level = JsonSerializer.Deserialize<LevelFileData>(json, JsonOptions) ?? new();
        }

        // 读取所有 .shape.json 文件
        foreach (var file in Directory.GetFiles(directoryPath, "*.shape.json"))
        {
            var json = File.ReadAllText(file);
            var shape = JsonSerializer.Deserialize<ShapeFileData>(json, JsonOptions);
            if (shape != null)
            {
                package.CustomShapes[Path.GetFileName(file)] = shape;
            }
        }

        return package;
    }

    /// <summary>
    /// 解析关卡包为 LevelData（需要提供形状解析器）。
    /// </summary>
    /// <param name="builtinShapeResolver">内置形状解析器，根据名称返回 ShapeData</param>
    public LevelData ToLevelData(Func<string, ShapeData?> builtinShapeResolver)
    {
        var shapes = new List<ShapeData>();

        foreach (var shapeId in Level.ShapeIds)
        {
            if (!Metadata.ShapeIndex.TryGetValue(shapeId, out var source))
                throw new InvalidOperationException($"Shape '{shapeId}' not found in metadata.");

            var (isBuiltin, name) = LevelMetadata.ParseShapeSource(source);

            if (isBuiltin)
            {
                var shape = builtinShapeResolver(name)
                    ?? throw new InvalidOperationException($"Builtin shape '{name}' not found.");
                shapes.Add(shape);
            }
            else
            {
                if (!CustomShapes.TryGetValue(name, out var shapeFile))
                    throw new InvalidOperationException($"Custom shape file '{name}' not found.");
                shapes.Add(shapeFile.ToShapeData());
            }
        }

        return new LevelData
        {
            Id = Level.Id,
            Name = Level.Name,
            Difficulty = Level.Difficulty,
            Rows = Level.Rows,
            Cols = Level.Cols,
            Target = Level.ParseTarget(),
            Shapes = shapes.ToArray()
        };
    }

    /// <summary>
    /// 从 LevelData 创建关卡包。
    /// </summary>
    /// <param name="level">关卡数据</param>
    /// <param name="shapeInfos">形状信息列表 (shapeId, isBuiltin, builtinName/customShape, color)</param>
    public static LevelPackage FromLevelData(
        LevelData level,
        IEnumerable<(string ShapeId, bool IsBuiltin, string BuiltinName, ShapeFileData? CustomShape, string? Color)> shapeInfos)
    {
        var package = new LevelPackage();
        var shapeIds = new List<string>();

        foreach (var (shapeId, isBuiltin, builtinName, customShape, color) in shapeInfos)
        {
            shapeIds.Add(shapeId);

            if (isBuiltin)
            {
                package.Metadata.AddBuiltinShape(shapeId, builtinName, color);
            }
            else if (customShape != null)
            {
                var filename = $"{shapeId}.shape.json";
                package.Metadata.AddCustomShape(shapeId, filename, color);
                package.CustomShapes[filename] = customShape;
            }
        }

        package.Level = LevelFileData.FromLevelData(level, shapeIds.ToArray());
        package.Metadata.CreatedAt = DateTime.UtcNow.ToString("o");

        return package;
    }
}
