using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.EntityEffects;

[TestFixture]
[TestOf(typeof(AddObjectiveEntityEffectSystem))]
public sealed class AddObjectiveTest : GameTest
{
    private static readonly ProtoId<EntityEffectPrototype> EffectProtoId = "AddObjectiveTestEffect";

    [SidedDependency(Side.Server)] private SharedMindSystem _mind = default!;
    [SidedDependency(Side.Server)] private SharedEntityEffectsSystem _effects = default!;
    [SidedDependency(Side.Server)] private SharedRoleSystem _roles = default!;

    [TestPrototypes]
    private const string Prototypes = """
- type: entityEffect
  id: AddObjectiveTestEffect
  effects:
  - !type:AddObjective
    objective: "WizardSurviveObjective"
""";

    public override PoolSettings PoolSettings => new() { Connected = false };

    [Test]
    [RunOnSide(Side.Server)]
    public void AddObjectiveToMindTest()
    {
        var entity = SSpawn("MobHuman");
        var mind = _mind.CreateMind(null);
        _mind.TransferTo(mind, entity);
        _roles.MindAddRole(mind, "MindRoleWizard", mind.Comp, silent: true);

        Assert.That(mind.Comp.Objectives, Is.Empty);

        _effects.TryApplyEffect(entity, EffectProtoId);

        Assert.That(mind.Comp.Objectives, Has.Count.EqualTo(1));
        Assert.That(SComp<MetaDataComponent>(mind.Comp.Objectives[0]).EntityPrototype?.ID, Is.EqualTo("WizardSurviveObjective"));
    }
}
