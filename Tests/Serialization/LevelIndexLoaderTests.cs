using PictoMino.Core;
using PictoMino.Core.Serialization;

namespace PictoMino.Tests.Serialization;

[TestFixture]
public class LevelIndexLoaderTests
{
    private BuiltinShapeRegistry _registry = null!;

    [SetUp]
    public void SetUp()
    {
        _registry = BuiltinShapeRegistry.CreateStandard();
    }

    [Test]
    public void LoadIndex_ParsesCorrectly()
    {
        var json = """
        {
          "chapters": [
            {
              "id": "test",
              "name": "测试章节",
              "levels": ["level1.level", "level2.level"]
            }
          ]
        }
        """;
        var loader = new LevelIndexLoader(_registry, _ => null);

        var index = loader.LoadIndex(json);

        Assert.That(index, Is.Not.Null);
        Assert.That(index!.Chapters.Count, Is.EqualTo(1));
        Assert.That(index.Chapters[0].Id, Is.EqualTo("test"));
        Assert.That(index.Chapters[0].Levels.Count, Is.EqualTo(2));
    }

    [Test]
    public void LoadLevel_WithValidPackage_ReturnsLevelData()
    {
        var package = new LevelPackage
        {
            Level = new LevelFileData
            {
                Id = "test",
                Name = "Test Level",
                Rows = 2,
                Cols = 2,
                ShapeIds = new[] { "shape1" }
            }
        };
        package.Metadata.AddBuiltinShape("shape1", "O");
        var levelData = package.Save();

        var loader = new LevelIndexLoader(_registry, path => path == "test.level" ? levelData : null);
        var level = loader.LoadLevel("test.level");

        Assert.That(level, Is.Not.Null);
        Assert.That(level!.Id, Is.EqualTo("test"));
        Assert.That(level.Shapes.Length, Is.EqualTo(1));
    }

    [Test]
    public void LoadAllChapters_LoadsMultipleLevels()
    {
        var package1 = CreateTestPackage("level1", "关卡1");
        var package2 = CreateTestPackage("level2", "关卡2");

        var files = new Dictionary<string, byte[]>
        {
            ["level1.level"] = package1.Save(),
            ["level2.level"] = package2.Save()
        };

        var indexJson = """
        {
          "chapters": [{
            "id": "ch1",
            "name": "章节1",
            "levels": ["level1.level", "level2.level"]
          }]
        }
        """;

        var loader = new LevelIndexLoader(_registry, path => files.GetValueOrDefault(path));
        var chapters = loader.LoadAllChapters(indexJson);

        Assert.That(chapters.Count, Is.EqualTo(1));
        Assert.That(chapters[0].Levels.Length, Is.EqualTo(2));
        Assert.That(chapters[0].Levels[0].Name, Is.EqualTo("关卡1"));
        Assert.That(chapters[0].Levels[1].Name, Is.EqualTo("关卡2"));
    }

    private static LevelPackage CreateTestPackage(string id, string name)
    {
        var package = new LevelPackage
        {
            Level = new LevelFileData
            {
                Id = id,
                Name = name,
                Rows = 2,
                Cols = 2,
                ShapeIds = new[] { "s1" }
            }
        };
        package.Metadata.AddBuiltinShape("s1", "O");
        return package;
    }
}
