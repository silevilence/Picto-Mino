using System.Collections.Generic;
using System.Text.Json;
using Godot;
using PictoMino.Core;
using PictoMino.Core.Serialization;

namespace PictoMino.View;

/// <summary>
/// Godot 关卡加载器。从 res:// 路径加载关卡文件。
/// </summary>
public static class GodotLevelLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static BuiltinShapeRegistry? _shapeRegistry;

    /// <summary>
    /// 获取或创建内置形状注册表。
    /// </summary>
    public static BuiltinShapeRegistry GetShapeRegistry()
    {
        if (_shapeRegistry == null)
        {
            _shapeRegistry = BuiltinShapeRegistry.CreateStandard();
            LoadBuiltinShapesFromResources();
        }
        return _shapeRegistry;
    }

    /// <summary>
    /// 从 res://Shapes/shapeIndex.json 加载内置形状。
    /// </summary>
    private static void LoadBuiltinShapesFromResources()
    {
        if (_shapeRegistry == null) return;

        var indexJson = LoadTextFile("res://Shapes/shapeIndex.json");
        if (indexJson == null)
        {
            GD.Print("GodotLevelLoader: res://Shapes/shapeIndex.json not found, using standard shapes only.");
            return;
        }

        Dictionary<string, string>? shapeIndex;
        try
        {
            shapeIndex = JsonSerializer.Deserialize<Dictionary<string, string>>(indexJson, JsonOptions);
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"GodotLevelLoader: Failed to parse shapeIndex.json: {e.Message}");
            return;
        }

        if (shapeIndex == null) return;

        foreach (var (id, fileName) in shapeIndex)
        {
            var path = $"res://Shapes/{fileName}";
            var json = LoadTextFile(path);
            if (json != null)
            {
                try
                {
                    _shapeRegistry.RegisterFromJson(json);
                    GD.Print($"GodotLevelLoader: Loaded shape '{id}' from {fileName}");
                }
                catch (System.Exception e)
                {
                    GD.PrintErr($"GodotLevelLoader: Failed to load shape {fileName}: {e.Message}");
                }
            }
            else
            {
                GD.PrintErr($"GodotLevelLoader: Shape file not found: {path}");
            }
        }
    }

    /// <summary>
    /// 从 res://Levels/index.json 加载所有章节。
    /// </summary>
    public static List<LevelChapter> LoadAllChapters()
    {
        var indexJson = LoadTextFile("res://Levels/index.json");
        if (indexJson == null)
        {
            GD.PrintErr("GodotLevelLoader: res://Levels/index.json not found.");
            return new List<LevelChapter>();
        }

        var registry = GetShapeRegistry();
        var loader = new LevelIndexLoader(registry, path => LoadBinaryFile($"res://Levels/{path}"));

        return loader.LoadAllChapters(indexJson);
    }

    /// <summary>
    /// 加载单个关卡文件。
    /// </summary>
    public static LevelData? LoadLevel(string path)
    {
        var data = LoadBinaryFile(path);
        if (data == null) return null;

        try
        {
            var package = LevelPackage.Load(data);
            return package.ToLevelData(GetShapeRegistry().CreateResolver());
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"GodotLevelLoader: Failed to load level {path}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从外部文件加载关卡（用于打开文件对话框）。
    /// </summary>
    public static LevelData? LoadLevelFromExternalFile(string absolutePath)
    {
        try
        {
            var package = LevelPackage.LoadFromFile(absolutePath);
            return package.ToLevelData(GetShapeRegistry().CreateResolver());
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"GodotLevelLoader: Failed to load external level {absolutePath}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 读取文本文件。
    /// </summary>
    private static string? LoadTextFile(string path)
    {
        if (!FileAccess.FileExists(path)) return null;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"GodotLevelLoader: Cannot open {path}: {FileAccess.GetOpenError()}");
            return null;
        }

        return file.GetAsText();
    }

    /// <summary>
    /// 读取二进制文件。
    /// </summary>
    private static byte[]? LoadBinaryFile(string path)
    {
        if (!FileAccess.FileExists(path)) return null;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"GodotLevelLoader: Cannot open {path}: {FileAccess.GetOpenError()}");
            return null;
        }

        return file.GetBuffer((long)file.GetLength());
    }
}
