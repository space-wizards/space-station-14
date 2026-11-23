using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.EntityEffects;

[TestFixture]
[TestOf(typeof(AddObjectiveEntityEffectSystem))]
public sealed class AddObjectiveTest : GameTest
{
    [SidedDependency(Side.Server)] private SharedMindSystem _sMind = default!;
    [SidedDependency(Side.Server)] private SharedEntityEffectsSystem _sEffects = default!;

    private const string EffectProtoId = "AddObjectiveTestEffect";
    private const string ObjectiveProtoId = "AddObjectiveTestObjective";

    [TestPrototypes]
    private static readonly string Prototypes = $@"
- type: entity
  id: {ObjectiveProtoId}
  name: Get Funky
  description: The Space Wizards Federation want you to get funky!
  components:
  - type: Objective
    difficulty: 1
    issuer: SpaceWizardsFederation
    icon:
        sprite: Clothing/Head/Hats/wizard_blue.rsi
        state: icon
  - type: SurviveCondition

- type: entityEffect
  id: {EffectProtoId}
  effects:
  - !type:AddObjective
    objective: {ObjectiveProtoId}
";

    public override PoolSettings PoolSettings => new() { Connected = false };


    [Test]
    [RunOnSide(Side.Server)]
    [Description("Checks that AddObjective properly results in an objective being added.")]
    public void AddObjectiveToMindTest()
    {
        var entity = SSpawn("MobHuman");
        var mind = _sMind.CreateMind(null);
        _sMind.TransferTo(mind, entity);
        Assume.That(mind.Comp.Objectives, Is.Empty);

        _sEffects.TryApplyEffect(entity, EffectProtoId);
        Assert.Multiple(() =>
        {
            Assert.That(mind.Comp.Objectives, Has.Count.EqualTo(1));
            Assert.That(SComp<MetaDataComponent>(mind.Comp.Objectives[0]).EntityPrototype?.ID, Is.EqualTo(ObjectiveProtoId));
        });
    }
}
