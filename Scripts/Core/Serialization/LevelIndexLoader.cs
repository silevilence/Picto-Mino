using System.Text.Encodings.Web;
using System.Text.Json;

namespace PictoMino.Core.Serialization;

/// <summary>
/// 关卡索引加载器。处理 index.json 和关卡文件的加载。
/// </summary>
public class LevelIndexLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly BuiltinShapeRegistry _shapeRegistry;
    private readonly Func<string, byte[]?> _fileReader;

    /// <summary>
    /// 创建关卡索引加载器。
    /// </summary>
    /// <param name="shapeRegistry">内置形状注册表</param>
    /// <param name="fileReader">文件读取函数，参数为相对路径，返回文件内容</param>
    public LevelIndexLoader(BuiltinShapeRegistry shapeRegistry, Func<string, byte[]?> fileReader)
    {
        _shapeRegistry = shapeRegistry;
        _fileReader = fileReader;
    }

    /// <summary>
    /// 加载关卡索引。
    /// </summary>
    /// <param name="indexJson">index.json 内容</param>
    public LevelIndexData? LoadIndex(string indexJson)
    {
        return JsonSerializer.Deserialize<LevelIndexData>(indexJson, JsonOptions);
    }

    /// <summary>
    /// 加载单个关卡。
    /// </summary>
    /// <param name="levelPath">关卡文件路径</param>
    public LevelData? LoadLevel(string levelPath)
    {
        var data = _fileReader(levelPath);
        if (data == null) return null;

        var package = LevelPackage.Load(data);
        return package.ToLevelData(_shapeRegistry.CreateResolver());
    }

    /// <summary>
    /// 加载所有章节。
    /// </summary>
    /// <param name="indexJson">index.json 内容</param>
    /// <param name="basePath">关卡文件基础路径</param>
    public List<LevelChapter> LoadAllChapters(string indexJson, string basePath = "")
    {
        var index = LoadIndex(indexJson);
        if (index == null) return new List<LevelChapter>();

        var chapters = new List<LevelChapter>();

        foreach (var chapterIndex in index.Chapters)
        {
            var levels = new List<LevelData>();

            foreach (var levelFile in chapterIndex.Levels)
            {
                var path = string.IsNullOrEmpty(basePath) ? levelFile : $"{basePath}/{levelFile}";
                var level = LoadLevel(path);
                if (level != null)
                {
                    levels.Add(level);
                }
            }

            if (levels.Count > 0)
            {
                chapters.Add(new LevelChapter
                {
                    Id = chapterIndex.Id,
                    Name = chapterIndex.Name,
                    Levels = levels.ToArray()
                });
            }
        }

        return chapters;
    }
}
