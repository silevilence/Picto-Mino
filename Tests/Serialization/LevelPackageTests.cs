using PictoMino.Core;
using PictoMino.Core.Serialization;

namespace PictoMino.Tests.Serialization;

[TestFixture]
public class LevelPackageTests
{
    [Test]
    public void SaveAndLoad_RoundTrip_PreservesData()
    {
        var package = new LevelPackage
        {
            Level = new LevelFileData
            {
                Id = "test_01",
                Name = "测试关卡",
                Difficulty = 2,
                Rows = 3,
                Cols = 3,
                Target = new[] { "###", "#.#", "###" },
                ShapeIds = new[] { "shape1", "shape2" }
            },
            Metadata = new LevelMetadata
            {
                Version = 1,
                Author = "Test Author"
            }
        };
        package.Metadata.AddBuiltinShape("shape1", "I", "#FF0000");
        package.Metadata.AddCustomShape("shape2", "custom.shape.json", "#00FF00");
        package.CustomShapes["custom.shape.json"] = new ShapeFileData
        {
            Id = "custom",
            Name = "自定义",
            Matrix = new[] { "##", "##" }
        };

        var data = package.Save();
        var loaded = LevelPackage.Load(data);

        Assert.That(loaded.Level.Id, Is.EqualTo("test_01"));
        Assert.That(loaded.Level.Name, Is.EqualTo("测试关卡"));
        Assert.That(loaded.Level.Difficulty, Is.EqualTo(2));
        Assert.That(loaded.Level.Target, Is.EqualTo(new[] { "###", "#.#", "###" }));
        Assert.That(loaded.Metadata.Author, Is.EqualTo("Test Author"));
        Assert.That(loaded.Metadata.ShapeIndex["shape1"], Is.EqualTo("builtin:I"));
        Assert.That(loaded.Metadata.ShapeIndex["shape2"], Is.EqualTo("custom:custom.shape.json"));
        Assert.That(loaded.CustomShapes["custom.shape.json"].Matrix, Is.EqualTo(new[] { "##", "##" }));
    }

    [Test]
    public void ToLevelData_ResolvesShapes()
    {
        var package = new LevelPackage
        {
            Level = new LevelFileData
            {
                Id = "test",
                Name = "Test",
                Rows = 2,
                Cols = 2,
                ShapeIds = new[] { "builtin_shape", "custom_shape" }
            }
        };
        package.Metadata.AddBuiltinShape("builtin_shape", "O");
        package.Metadata.AddCustomShape("custom_shape", "my.shape.json");
        package.CustomShapes["my.shape.json"] = new ShapeFileData
        {
            Matrix = new[] { "##" }
        };

        var builtinO = new ShapeData(new bool[,] { { true, true }, { true, true } });
        ShapeData? ResolveBuiltin(string name) => name == "O" ? builtinO : null;

        var levelData = package.ToLevelData(ResolveBuiltin);

        Assert.That(levelData.Id, Is.EqualTo("test"));
        Assert.That(levelData.Shapes.Length, Is.EqualTo(2));
        Assert.That(levelData.Shapes[0].CellCount, Is.EqualTo(4)); // O shape
        Assert.That(levelData.Shapes[1].CellCount, Is.EqualTo(2)); // custom ##
    }

    [Test]
    public void LevelFileData_ParseTarget_ConvertsCorrectly()
    {
        var fileData = new LevelFileData
        {
            Rows = 3,
            Cols = 3,
            Target = new[] { "###", "#.#", "###" }
        };

        var target = fileData.ParseTarget();

        Assert.That(target, Is.Not.Null);
        Assert.That(target![0, 0], Is.True);
        Assert.That(target[1, 1], Is.False);
        Assert.That(target[2, 2], Is.True);
    }

    [Test]
    public void LevelFileData_ParseTarget_NullTarget_ReturnsNull()
    {
        var fileData = new LevelFileData { Rows = 3, Cols = 3, Target = null };

        var target = fileData.ParseTarget();

        Assert.That(target, Is.Null);
    }

    [Test]
    public void LevelMetadata_ParseShapeSource_Builtin()
    {
        var (isBuiltin, name) = LevelMetadata.ParseShapeSource("builtin:I");

        Assert.That(isBuiltin, Is.True);
        Assert.That(name, Is.EqualTo("I"));
    }

    [Test]
    public void LevelMetadata_ParseShapeSource_Custom()
    {
        var (isBuiltin, name) = LevelMetadata.ParseShapeSource("custom:my.shape.json");

        Assert.That(isBuiltin, Is.False);
        Assert.That(name, Is.EqualTo("my.shape.json"));
    }

    [Test]
    public void LevelMetadata_ParseShapeSource_Invalid_Throws()
    {
        Assert.Throws<FormatException>(() => LevelMetadata.ParseShapeSource("invalid"));
    }
}
