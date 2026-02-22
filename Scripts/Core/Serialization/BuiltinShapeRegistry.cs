using System.Text.Encodings.Web;
using System.Text.Json;

namespace PictoMino.Core.Serialization;

/// <summary>
/// 内置形状注册表。管理从文件加载的内置形状。
/// </summary>
public class BuiltinShapeRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly Dictionary<string, ShapeData> _shapes = new();
    private readonly Dictionary<string, ShapeFileData> _shapeFiles = new();

    /// <summary>所有已注册的形状 ID</summary>
    public IEnumerable<string> ShapeIds => _shapes.Keys;

    /// <summary>已注册形状数量</summary>
    public int Count => _shapes.Count;

    /// <summary>
    /// 注册形状。
    /// </summary>
    public void Register(string id, ShapeData shape)
    {
        _shapes[id] = shape;
    }

    /// <summary>
    /// 从 JSON 字符串注册形状。
    /// </summary>
    public void RegisterFromJson(string json)
    {
        var fileData = JsonSerializer.Deserialize<ShapeFileData>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize shape.");
        
        _shapeFiles[fileData.Id] = fileData;
        _shapes[fileData.Id] = fileData.ToShapeData();
    }

    /// <summary>
    /// 获取形状。
    /// </summary>
    public ShapeData? GetShape(string id)
    {
        return _shapes.TryGetValue(id, out var shape) ? shape : null;
    }

    /// <summary>
    /// 获取形状文件数据。
    /// </summary>
    public ShapeFileData? GetShapeFileData(string id)
    {
        return _shapeFiles.TryGetValue(id, out var data) ? data : null;
    }

    /// <summary>
    /// 尝试获取形状。
    /// </summary>
    public bool TryGetShape(string id, out ShapeData? shape)
    {
        return _shapes.TryGetValue(id, out shape);
    }

    /// <summary>
    /// 创建形状解析器函数（用于 LevelPackage.ToLevelData）。
    /// </summary>
    public Func<string, ShapeData?> CreateResolver()
    {
        return GetShape;
    }

    /// <summary>
    /// 创建包含标准多格骨牌的注册表。
    /// </summary>
    public static BuiltinShapeRegistry CreateStandard()
    {
        var registry = new BuiltinShapeRegistry();

        // 单格
        registry.Register("Dot", new ShapeData(new bool[,] { { true } }));

        // 两格
        registry.Register("I2", new ShapeData(new bool[,] { { true, true } }));

        // 三格
        registry.Register("I3", new ShapeData(new bool[,] { { true, true, true } }, 0, 1));
        registry.Register("L3", new ShapeData(new bool[,] { { true, true }, { true, false } }));

        // 四格 (Tetrominoes)
        registry.Register("I", new ShapeData(new bool[,] { { true, true, true, true } }, 0, 1));
        registry.Register("O", new ShapeData(new bool[,] { { true, true }, { true, true } }));
        registry.Register("T", new ShapeData(new bool[,] { { true, true, true }, { false, true, false } }, 0, 1));
        registry.Register("L", new ShapeData(new bool[,] { { true, false }, { true, false }, { true, true } }, 1, 0));
        registry.Register("J", new ShapeData(new bool[,] { { false, true }, { false, true }, { true, true } }, 1, 1));
        registry.Register("S", new ShapeData(new bool[,] { { false, true, true }, { true, true, false } }, 0, 1));
        registry.Register("Z", new ShapeData(new bool[,] { { true, true, false }, { false, true, true } }, 0, 1));

        return registry;
    }
}
