#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.IntegrationTests.NUnit.Operators;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.GameTestTests;

public sealed class ConstraintsTests : GameTest
{
    private const string TestProtoId = "TestProtoId";

    [TestPrototypes]
    private const string TestPrototypes = $@"
- type: entity
  id: {TestProtoId}
  components:
  - type: MetaData
";

    [Test]
    [TestOf(typeof(CompExistsConstraint))]
    [Description("Ensures that a freshly spawned entity matches a constraint stating it has MetaData.")]
    [RunOnSide(Side.Server)]
    public void CompPositive()
    {
        var ent = SSpawn(null);

        Assert.That(ent, Has.Comp<MetaDataComponent>(Server));
    }

    [Test]
    [TestOf(typeof(CompOperator))]
    [Description("Ensures that NUnit property access works on Comp constraints.")]
    [RunOnSide(Side.Server)]
    public void CompPropertyAccess()
    {
        var ent = SSpawn(null);

        Assert.That(ent,
            Has
                .Comp<MetaDataComponent>(Server)
                .Property(nameof(MetaDataComponent.EntityDeleted))
                .EqualTo(false)
        );
    }

    [Test]
    [TestOf(typeof(CompExistsConstraint))]
    [Description("Ensures that a freshly spawned entity does not match a constraint stating it has some odd component.")]
    [RunOnSide(Side.Server)]
    public void CompNegative()
    {
        var ent = SSpawn(null);

        // Arbitrary pick.
        Assert.That(ent, Has.No.Comp<EyeComponent>(Server));
    }

    [Test]
    [TestOf(typeof(CompExistsConstraint))]
    [Description("Ensures that a test entity prototype matches a constraint stating it has a specific component.")]
    [RunOnSide(Side.Server)]
    public void ProtoCompPositive()
    {
        var proto = SProtoMan.Index(TestProtoId);

        Assert.That(proto, Has.Comp<MetaDataComponent>(Server));
    }

    [Test]
    [TestOf(typeof(CompExistsConstraint))]
    [Description("Ensures that a test entity prototype (specified by ID) matches a constraint stating it has a specific component.")]
    [RunOnSide(Side.Server)]
    public void ProtoIdCompPositive()
    {
        Assert.That(TestProtoId, Has.Comp<MetaDataComponent>(Server));
    }

    [Test]
    [TestOf(typeof(CompExistsConstraint))]
    [Description("Ensures that a test entity prototype does not match a constraint stating it has an arbitrary component.")]
    [RunOnSide(Side.Server)]
    public void ProtoCompNegative()
    {
        var proto = SProtoMan.Index(TestProtoId);

        Assert.That(proto, Has.No.Comp<EyeComponent>(Server));
    }

    [Test]
    [TestOf(typeof(CompOperator))]
    [Description("Ensures that NUnit property access works on Comp constraints with EntityPrototypes.")]
    [RunOnSide(Side.Server)]
    public void ProtoCompPropertyAccess()
    {
        var proto = SProtoMan.Index(TestProtoId);

        Assert.That(proto,
            Has
                .Comp<MetaDataComponent>(Server)
                .Property(nameof(MetaDataComponent.EntityDeleted))
                .EqualTo(false)
        );
    }

    [Test]
    [TestOf(typeof(LifeStageConstraint))]
    [Description("Ensures that a freshly deleted entity is deleted to constraints.")]
    [RunOnSide(Side.Server)]
    public void DeletedPositive()
    {
        var ent = SSpawn(null);

        SDeleteNow(ent);

        Assert.That(ent, Is.Deleted(Server));
    }

    [Test]
    [TestOf(typeof(LifeStageConstraint))]
    [RunOnSide(Side.Server)]
    [Description("Entities that never existed are currently considered deleted by constraints.")]
    public void DeletedNeverExisted()
    {
        // We'll never spawn this many ents in tests without it taking all damn day.
        var ent = new EntityUid(int.MaxValue / 2);

        Assert.That(ent, Is.Deleted(Server), "Entites that never existed still count as deleted.");
    }

    [Test]
    [TestOf(typeof(LifeStageConstraint))]
    [Description("Entities that are alive do not count as deleted.")]
    [RunOnSide(Side.Server)]
    public void DeletedNegative()
    {
        var ent = SSpawn(null);

        Assert.That(ent, Is.Not.Deleted(Server));
    }
}
