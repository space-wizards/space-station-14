#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Security;

public sealed class StunBatonTests : InteractionTest
{
    private static readonly EntProtoId StunBatonProtoId = "Stunbaton";
    private static readonly EntProtoId HumanProtoId = "MobHuman";

    // If you are rebalancing stun batons you will have to change this number.
    private const int NumberOfHitsToStun = 3;

    /// <summary>
    /// Checks that an activated stun baton stuns the target.
    /// </summary>
    [Test]
    public async Task StunBatonTest()
    {
        var batterySystem = SEntMan.System<PredictedBatterySystem>();
        // Prevent the test mob from suffocating.
        await AddAtmosphere();

        // Spawn a stun baton in the player's hands and turn it on.
        var baton = await PlaceInHands(StunBatonProtoId, enableToggleable: true);
        var sBaton = ToServer(baton);
        var batonStaminaDamage = Comp<StaminaDamageOnHitComponent>(baton).Damage;
        var batonComp = Comp<StunbatonComponent>(baton);
        var batonIntialCharges = batterySystem.GetRemainingUses(sBaton, batonComp.EnergyPerUse);
        var batonMaxCharges = batterySystem.GetMaxUses(sBaton, batonComp.EnergyPerUse);

        Assert.Multiple(() =>
        {
            Assert.That(batonMaxCharges, Is.GreaterThan(0), "Stun baton had no charges.");
            Assert.That(batonIntialCharges, Is.EqualTo(batonMaxCharges), "Stun baton was not fully charged when spawned.");
        });

        // Spawn a target mob.
        await SpawnTarget(HumanProtoId);
        var standingStateComp = Comp<StandingStateComponent>();
        var damageableComp = Comp<DamageableComponent>();
        var staminaComp = Comp<StaminaComponent>();

        Assert.Multiple(() =>
        {
            Assert.That(HasComp<KnockedDownComponent>(), Is.False, "Target mob spawned knocked down.");
            Assert.That(HasComp<StunnedComponent>(), Is.False, "Target mob spawned stunned.");
            Assert.That(standingStateComp.Standing, Is.True, "Target mob was not standing when spawned.");
            Assert.That(damageableComp.TotalDamage, Is.EqualTo(FixedPoint2.Zero), "Target mob spawned with damage.");
            Assert.That(staminaComp.StaminaDamage, Is.Zero, "Target mob spawned with stamina damage.");
        });

        // Melee attack.
        await SetCombatMode(true);
        await RunSeconds(2); // Weapon cooldown.
        await AttemptLightAttack();

        // Not stunned yet.
        Assert.Multiple(() =>
        {
            Assert.That(HasComp<KnockedDownComponent>(), Is.False, "Target mob was knocked down after one hit.");
            Assert.That(HasComp<StunnedComponent>(), Is.False, "Target mob was stunned after one hit.");
            Assert.That(standingStateComp.Standing, Is.True, "Target mob was not standing after one hit.");
            Assert.That(damageableComp.TotalDamage, Is.EqualTo(FixedPoint2.Zero), "Activated stun baton caused damage.");
            Assert.That(staminaComp.StaminaDamage, Is.EqualTo(batonStaminaDamage), "Target mob did not take the correct amount of stamina damage.");
            Assert.That(batterySystem.GetRemainingUses(sBaton, batonComp.EnergyPerUse), Is.EqualTo(batonMaxCharges - 1), "Stun baton did not loose a charge when used.");
        });

        // Attack until stunned.
        for (var i = 0; i < NumberOfHitsToStun - 1; i++)
        {
            await RunSeconds(2); // Weapon cooldown.
            await AttemptLightAttack();
        }

        // Check all components to see if we are stunned now.
        Assert.Multiple(() =>
        {
            Assert.That(HasComp<KnockedDownComponent>(), Is.True, "Target mob was not knocked down from the expected number of stun baton hits.");
            Assert.That(HasComp<StunnedComponent>(), Is.True, "Target mob was not stunned from the expected number of stun baton hits.");
            Assert.That(standingStateComp.Standing, Is.False, "Target mob was not downed from the expected number of stun baton hits.");
            Assert.That(damageableComp.TotalDamage, Is.EqualTo(FixedPoint2.Zero), "Activated stun baton caused damage.");
            Assert.That(batterySystem.GetRemainingUses(sBaton, batonComp.EnergyPerUse), Is.EqualTo(batonMaxCharges - NumberOfHitsToStun), "Stun baton did not loose the correct charge when stunning.");
        });
    }

    /// <summary>
    /// Checks that a deactivated stun baton does not stun the target.
    /// </summary>
    [Test]
    public async Task HarmBatonTest()
    {
        var batterySystem = SEntMan.System<PredictedBatterySystem>();
        // Prevent the test mob from suffocating.
        await AddAtmosphere();

        // Spawn a stun baton in the player's hands without turning it on.
        var baton = await PlaceInHands(StunBatonProtoId, enableToggleable: false);
        var sBaton = ToServer(baton);
        var batonStaminaDamage = Comp<StaminaDamageOnHitComponent>(baton).Damage;
        var batonComp = Comp<StunbatonComponent>(baton);
        var batonIntialCharges = batterySystem.GetRemainingUses(sBaton, batonComp.EnergyPerUse);
        var batonMaxCharges = batterySystem.GetMaxUses(sBaton, batonComp.EnergyPerUse);

        Assert.Multiple(() =>
        {
            Assert.That(batonMaxCharges, Is.GreaterThan(0), "Stun baton had no charges.");
            Assert.That(batonIntialCharges, Is.EqualTo(batonMaxCharges), "Stun baton was not fully charged when spawned.");
        });

        // Spawn a target mob.
        await SpawnTarget(HumanProtoId);
        var standingStateComp = Comp<StandingStateComponent>();
        var damageableComp = Comp<DamageableComponent>();
        var staminaComp = Comp<StaminaComponent>();

        Assert.Multiple(() =>
        {
            Assert.That(HasComp<KnockedDownComponent>(), Is.False, "Target mob spawned knocked down.");
            Assert.That(HasComp<StunnedComponent>(), Is.False, "Target mob spawned stunned.");
            Assert.That(standingStateComp.Standing, Is.True, "Target mob was not standing when spawned.");
            Assert.That(damageableComp.TotalDamage, Is.EqualTo(FixedPoint2.Zero), "Target mob spawned with damage.");
            Assert.That(staminaComp.StaminaDamage, Is.Zero, "Target mob spawned with stamina damage.");
        });

        // Attack until the target would be stunned if the baton was activated.
        await SetCombatMode(true);
        for (var i = 0; i < NumberOfHitsToStun; i++)
        {
            await RunSeconds(2); // Weapon cooldown.
            await AttemptLightAttack();
        }

        // Check all components to see if we are stunned now.
        Assert.Multiple(() =>
        {
            Assert.That(HasComp<KnockedDownComponent>(), Is.False, "Target mob was knocked down from harmbaton attacks.");
            Assert.That(HasComp<StunnedComponent>(), Is.False, "Target mob was stunned from harmbaton attacks.");
            Assert.That(standingStateComp.Standing, Is.True, "Target mob was downed from harmbaton attacks.");
            Assert.That(damageableComp.TotalDamage, Is.GreaterThan(FixedPoint2.Zero), "Deactivated stun baton did not cause damage.");
            Assert.That(batterySystem.GetRemainingUses(sBaton, batonComp.EnergyPerUse), Is.EqualTo(batonMaxCharges), "Stun baton lost charge while deactivated.");
        });
    }

    /// <summary>
    /// Checks that missing attack with a stun baton does not cost any charge.
    /// </summary>
    [Test]
    public async Task StunBatonMissTest()
    {
        var batterySystem = SEntMan.System<PredictedBatterySystem>();

        // Spawn a stun baton in the player's hands and turn it on.
        var baton = await PlaceInHands(StunBatonProtoId, enableToggleable: true);
        var sBaton = ToServer(baton);
        var batteryComp = Comp<PredictedBatteryComponent>(baton);
        var batonIntialCharge = batterySystem.GetCharge(sBaton);

        Assert.Multiple(() =>
        {
            Assert.That(batonIntialCharge, Is.GreaterThan(0), "Stun baton had no charge.");
            Assert.That(batonIntialCharge, Is.EqualTo(batteryComp.MaxCharge), "Stun baton was not fully charged when spawned.");
        });

        // Missing melee attack.
        await SetCombatMode(true);
        await RunSeconds(2); // Weapon cooldown.
        await AttemptLightAttackMiss();

        var batonNewCharge = batterySystem.GetCharge(sBaton);
        Assert.That(batonNewCharge, Is.EqualTo(batonIntialCharge), "Stun baton lost charge when missing an attack.");
    }
}
