using PictoMino.Core;
using PictoMino.Core.Serialization;

namespace PictoMino.Tests.Serialization;

[TestFixture]
public class BuiltinShapeRegistryTests
{
    [Test]
    public void CreateStandard_RegistersAllTetrominoes()
    {
        var registry = BuiltinShapeRegistry.CreateStandard();

        Assert.That(registry.Count, Is.GreaterThanOrEqualTo(11));
        Assert.That(registry.GetShape("I"), Is.Not.Null);
        Assert.That(registry.GetShape("O"), Is.Not.Null);
        Assert.That(registry.GetShape("T"), Is.Not.Null);
        Assert.That(registry.GetShape("L"), Is.Not.Null);
        Assert.That(registry.GetShape("J"), Is.Not.Null);
        Assert.That(registry.GetShape("S"), Is.Not.Null);
        Assert.That(registry.GetShape("Z"), Is.Not.Null);
    }

    [Test]
    public void RegisterFromJson_ParsesCorrectly()
    {
        var registry = new BuiltinShapeRegistry();
        var json = """{"id":"Test","name":"测试","matrix":["##","#."]}""";

        registry.RegisterFromJson(json);

        var shape = registry.GetShape("Test");
        Assert.That(shape, Is.Not.Null);
        Assert.That(shape!.CellCount, Is.EqualTo(3));
    }

    [Test]
    public void CreateResolver_ReturnsWorkingFunction()
    {
        var registry = BuiltinShapeRegistry.CreateStandard();
        var resolver = registry.CreateResolver();

        Assert.That(resolver("O"), Is.Not.Null);
        Assert.That(resolver("NonExistent"), Is.Null);
    }
}
