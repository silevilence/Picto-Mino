using PictoMino.Core;
using PictoMino.Core.Serialization;

namespace PictoMino.Tests.Serialization;

/// <summary>
/// 生成教程关卡文件的工具测试。
/// 运行此测试会在 Levels 目录生成 .level 文件。
/// </summary>
[TestFixture]
public class TutorialLevelGenerator
{
    private static readonly string LevelsDir = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "..", "Levels");

    [Test]
    [Explicit("Run manually to generate tutorial level files")]
    public void GenerateTutorialLevels()
    {
        Directory.CreateDirectory(LevelsDir);

        GenerateLevel01();
        GenerateLevel02();
        GenerateLevel03();
        GenerateLevel04();
        GenerateLevel05();

        TestContext.WriteLine($"Generated tutorial levels in: {Path.GetFullPath(LevelsDir)}");
    }

    private void GenerateLevel01()
    {
        var package = new LevelPackage
        {
            Level = new LevelFileData
            {
                Id = "tutorial_01",
                Name = "初识拼图",
                Difficulty = 1,
                Rows = 2,
                Cols = 2,
                ShapeIds = new[] { "shape1" }
            }
        };
        package.Metadata.AddBuiltinShape("shape1", "O", "#4CAF50");
        package.Metadata.Author = "Picto Mino";
        package.Metadata.Description = "学习基本操作：放置一个 2x2 方块";
        SavePackage(package, "tutorial_01.level");
    }

    private void GenerateLevel02()
    {
        var package = new LevelPackage
        {
            Level = new LevelFileData
            {
                Id = "tutorial_02",
                Name = "初试身手",
                Difficulty = 1,
                Rows = 2,
                Cols = 3,
                ShapeIds = new[] { "shape1", "shape2" }
            }
        };
        package.Metadata.AddBuiltinShape("shape1", "I3", "#2196F3");
        package.Metadata.AddBuiltinShape("shape2", "I3", "#FF9800");
        package.Metadata.Author = "Picto Mino";
        package.Metadata.Description = "放置两个三格条";
        SavePackage(package, "tutorial_02.level");
    }

    private void GenerateLevel03()
    {
        var package = new LevelPackage
        {
            Level = new LevelFileData
            {
                Id = "tutorial_03",
                Name = "L形初探",
                Difficulty = 1,
                Rows = 3,
                Cols = 3,
                ShapeIds = new[] { "shape1", "shape2", "shape3" }
            }
        };
        package.Metadata.AddBuiltinShape("shape1", "L", "#E91E63");
        package.Metadata.AddCustomShape("shape2", "I3v.shape.json", "#9C27B0");
        package.Metadata.AddBuiltinShape("shape3", "I2", "#00BCD4");
        package.CustomShapes["I3v.shape.json"] = new ShapeFileData
        {
            Id = "I3v",
            Name = "竖三格条",
            Matrix = new[] { "#", "#", "#" },
            AnchorRow = 1,
            AnchorCol = 0
        };
        package.Metadata.Author = "Picto Mino";
        package.Metadata.Description = "学习使用 L 形方块";
        SavePackage(package, "tutorial_03.level");
    }

    private void GenerateLevel04()
    {
        var package = new LevelPackage
        {
            Level = new LevelFileData
            {
                Id = "tutorial_04",
                Name = "方块组合",
                Difficulty = 2,
                Rows = 4,
                Cols = 4,
                ShapeIds = new[] { "shape1", "shape2", "shape3", "shape4" }
            }
        };
        package.Metadata.AddBuiltinShape("shape1", "O", "#F44336");
        package.Metadata.AddBuiltinShape("shape2", "O", "#4CAF50");
        package.Metadata.AddBuiltinShape("shape3", "O", "#2196F3");
        package.Metadata.AddBuiltinShape("shape4", "O", "#FFEB3B");
        package.Metadata.Author = "Picto Mino";
        package.Metadata.Description = "用四个 O 形填满 4x4 棋盘";
        SavePackage(package, "tutorial_04.level");
    }

    private void GenerateLevel05()
    {
        var package = new LevelPackage
        {
            Level = new LevelFileData
            {
                Id = "tutorial_05",
                Name = "T形挑战",
                Difficulty = 2,
                Rows = 4,
                Cols = 5,
                ShapeIds = new[] { "shape1", "shape2", "shape3", "shape4", "shape5" }
            }
        };
        package.Metadata.AddBuiltinShape("shape1", "T", "#9C27B0");
        package.Metadata.AddBuiltinShape("shape2", "T", "#E91E63");
        package.Metadata.AddBuiltinShape("shape3", "O", "#00BCD4");
        package.Metadata.AddBuiltinShape("shape4", "O", "#8BC34A");
        package.Metadata.AddBuiltinShape("shape5", "I", "#FF5722");
        package.Metadata.Author = "Picto Mino";
        package.Metadata.Description = "综合挑战：T形、O形和 I 形组合";
        SavePackage(package, "tutorial_05.level");
    }

    private void SavePackage(LevelPackage package, string filename)
    {
        var path = Path.Combine(LevelsDir, filename);
        package.SaveToFile(path);
        TestContext.WriteLine($"Generated: {filename}");
    }
}
