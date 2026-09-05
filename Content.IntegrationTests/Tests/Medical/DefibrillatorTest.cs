#nullable enable
using System.Linq;
using Content.IntegrationTests.Tests.Helpers;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Medical;

/// <summary>
/// Tests for defibrilators.
/// </summary>
[TestOf(typeof(DefibrillatorComponent))]
public sealed class DefibrillatorTest : InteractionTest
{
    private sealed class SpeechListenerSystem : TestListenerSystem<EntitySpokeEvent>;

    // We need two hands to use a defbrillator.
    protected override string PlayerPrototype => "MobHuman";

    private static readonly EntProtoId DefibrillatorProtoId = "Defibrillator";
    private static readonly EntProtoId TargetProtoId = "MobHuman";
    private static readonly EntProtoId BedProtoId = "Bed";
    private static readonly ProtoId<DamageTypePrototype> BluntDamageTypeId = "Blunt";

    /// <summary>
    /// Kills a target mob, heals them and then revives them with a defibrillator.
    /// </summary>
    [Test]
    public async Task KillAndReviveTest()
    {
        var damageableSystem = SEntMan.System<DamageableSystem>();
        var mobThresholdsSystem = SEntMan.System<MobThresholdSystem>();

        // Don't let the player and target suffocate.
        await AddAtmosphere();

        await SpawnTarget(TargetProtoId);

        var targetMobState = Comp<MobStateComponent>();
        var targetDamageable = Comp<DamageableComponent>();

        // Check that the target has no damage and is not crit or dead.
        Assert.Multiple(() =>
        {
            Assert.That(targetMobState.CurrentState, Is.EqualTo(MobState.Alive), "Target mob was not alive when spawned.");
            Assert.That(damageableSystem.GetTotalDamage(STarget!.Value), Is.EqualTo(FixedPoint2.Zero), "Target mob was damaged when spawned.");
        });

        // Get the damage needed to kill or crit the target.
        var critThreshold = mobThresholdsSystem.GetThresholdForState(STarget.Value, MobState.Critical);
        var deathThreshold = mobThresholdsSystem.GetThresholdForState(STarget.Value, MobState.Dead);
        var critDamage = new DamageSpecifier(ProtoMan.Index(BluntDamageTypeId), (critThreshold + deathThreshold) / 2);
        var deathDamage = new DamageSpecifier(ProtoMan.Index(BluntDamageTypeId), deathThreshold);

        // Kill the target by applying blunt damage.
        await Server.WaitPost(() => damageableSystem.SetDamage((STarget.Value, targetDamageable), deathDamage));
        await RunTicks(3);

        // Check that the target is dead.
        Assert.Multiple(() =>
        {
            Assert.That(targetMobState.CurrentState, Is.EqualTo(MobState.Dead), "Target mob did not die from deadly damage amount.");
            Assert.That(damageableSystem.GetTotalDamage(STarget!.Value), Is.EqualTo(deathThreshold), "Target mob had the wrong total damage amount after being killed.");
        });

        // Spawn a defib and activate it.
        var defib = await PlaceInHands(DefibrillatorProtoId, enableToggleable: true);
        var cooldown = Comp<DefibrillatorComponent>(defib).ZapDelay;

        // Wait for the cooldown.
        await RunSeconds((float)cooldown.TotalSeconds);

        // ZAP!
        await Interact();

        // Check that the target is still dead since it is over the crit threshold.
        // And it should have taken some extra damage.
        Assert.Multiple(() =>
        {
            Assert.That(targetMobState.CurrentState, Is.EqualTo(MobState.Dead), "Target mob was revived despite being over the death damage threshold.");
            Assert.That(damageableSystem.GetTotalDamage(STarget!.Value), Is.GreaterThan(deathThreshold), "Target mob did not take damage from being defibrillated.");
        });

        // Set the damage halfway between the crit and death thresholds so that the target can be revived.
        await Server.WaitPost(() => damageableSystem.SetDamage((STarget.Value, targetDamageable), critDamage));
        await RunTicks(3);

        // Check that the target is still dead.
        Assert.That(targetMobState.CurrentState, Is.EqualTo(MobState.Dead), "Target mob revived on its own.");

        // ZAP!
        await RunSeconds((float)cooldown.TotalSeconds);
        await Interact();

        // The target should be revived, but in crit.
        Assert.That(targetMobState.CurrentState, Is.EqualTo(MobState.Critical), "Target mob was not revived from being defibrillated.");
    }

    /// <summary>
    /// Revives a target mob that is strapped to a bed. The bed gets caught in the zap chain, but the defibrillator
    /// should only report on the patient, not complain that the bed is an inanimate object.
    /// </summary>
    [Test]
    public async Task ReviveBuckledTest()
    {
        var damageableSystem = SEntMan.System<DamageableSystem>();
        var mobThresholdsSystem = SEntMan.System<MobThresholdSystem>();
        var buckleSystem = SEntMan.System<SharedBuckleSystem>();
        var loc = Server.ResolveDependency<ILocalizationManager>();

        // Don't let the player and target suffocate.
        await AddAtmosphere();

        await SpawnTarget(TargetProtoId);
        var bed = ToServer(await Spawn(BedProtoId));

        var targetMobState = Comp<MobStateComponent>();
        var targetDamageable = Comp<DamageableComponent>();
        var targetBuckle = Comp<BuckleComponent>();

        await Server.WaitPost(() => buckleSystem.TryBuckle(STarget.Value, null, bed));
        await RunTicks(3);
        Assert.That(targetBuckle.BuckledTo, Is.EqualTo(bed), "Target mob was not buckled to the bed.");

        // Kill the target, then bring the damage back down to a revivable level.
        var critThreshold = mobThresholdsSystem.GetThresholdForState(STarget.Value, MobState.Critical);
        var deathThreshold = mobThresholdsSystem.GetThresholdForState(STarget.Value, MobState.Dead);
        var critDamage = new DamageSpecifier(ProtoMan.Index(BluntDamageTypeId), (critThreshold + deathThreshold) / 2);
        var deathDamage = new DamageSpecifier(ProtoMan.Index(BluntDamageTypeId), deathThreshold);

        await Server.WaitPost(() => damageableSystem.SetDamage((STarget.Value, targetDamageable), deathDamage));
        await RunTicks(3);
        await Server.WaitPost(() => damageableSystem.SetDamage((STarget.Value, targetDamageable), critDamage));
        await RunTicks(3);
        Assert.That(targetMobState.CurrentState, Is.EqualTo(MobState.Dead), "Target mob was not dead before being defibrillated.");

        // Spawn a defib, activate it and record everything it says.
        var defib = await PlaceInHands(DefibrillatorProtoId, enableToggleable: true);
        var sDefib = ToServer(defib);
        await Server.WaitPost(() => SEntMan.EnsureComponent<TestListenerComponent>(sDefib));
        var cooldown = Comp<DefibrillatorComponent>(defib).ZapDelay;
        await RunSeconds((float)cooldown.TotalSeconds);

        // ZAP!
        await Interact();

        Assert.That(targetMobState.CurrentState, Is.EqualTo(MobState.Critical), "Buckled target mob was not revived from being defibrillated.");

        // The dummy has no mind, so the defib complains about that. It must not also complain about the bed.
        var spoken = GetEvents<EntitySpokeEvent>(sDefib).Select(ev => ev.Message).ToList();
        Assert.That(spoken, Is.Not.Empty, "Defibrillator did not report on the patient.");
        Assert.That(spoken, Does.Not.Contain(loc.GetString("defibrillator-not-living")), "Defibrillator reported on the bed instead of the patient.");
    }
}
