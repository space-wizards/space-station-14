using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Weapons;

public sealed class WeaponTests : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman"; // The default test mob only has one hand
    private static readonly EntProtoId MobHuman = "MobHuman";
    private static readonly EntProtoId SniperMosin = "WeaponSniperMosin";

    [Test]
    public async Task GunRequiresWieldTest()
    {
        var gunSystem = SEntMan.System<SharedGunSystem>();
        var damageSystem = SEntMan.System<DamageableSystem>();

        await AddAtmosphere(); // prevent the Urist from suffocating

        var urist = await SpawnTarget(MobHuman);
        var damageComp = Comp<DamageableComponent>(urist);

        var mosinNet = await PlaceInHands(SniperMosin);
        var mosinEnt = ToServer(mosinNet);

        await Pair.RunSeconds(2f); // Guns have a cooldown when picking them up.

        Assert.That(HasComp<GunRequiresWieldComponent>(mosinNet),
            "Looks like you've removed the 'GunRequiresWield' component from the mosin sniper." +
            "If this was intentional, please update WeaponTests.cs to reflect this change!");

        var startAmmo = gunSystem.GetAmmoCount(mosinEnt);
        var wieldComp = Comp<WieldableComponent>(mosinNet);

        Assert.That(startAmmo, Is.GreaterThan(0), "Mosin was spawned with no ammo!");
        Assert.That(wieldComp.Wielded, Is.False, "Mosin was spawned wielded!");

        await AttemptShoot(urist, false); // should fail due to not being wielded
        var updatedAmmo = gunSystem.GetAmmoCount(mosinEnt);

        Assert.That(updatedAmmo,
            Is.EqualTo(startAmmo),
            "Mosin discharged ammo when the weapon should not have fired!");
        Assert.That(damageSystem.GetTotalDamage(ToServer(urist)),
            Is.EqualTo(FixedPoint2.Zero),
            "Urist took damage when the weapon should not have fired!");
        await UseInHand();

        Assert.That(wieldComp.Wielded, Is.True, "Mosin failed to wield when interacted with!");

        var gunComp = Comp<GunComponent>(mosinNet);
        await Pair.RunSeconds(1f); // Give time for the cancelled shot (from not being wield-able) to be released from CancellationHold
        Assert.That(gunComp.CancellationHold, Is.EqualTo(false), "Cancellation Hold was not released.");
        await AttemptShoot(urist);
        updatedAmmo = gunSystem.GetAmmoCount(mosinEnt);

        Assert.That(updatedAmmo, Is.EqualTo(startAmmo - 1), "Mosin failed to discharge appropriate amount of ammo!");
        Assert.That(damageSystem.GetTotalDamage(ToServer(urist)),
            Is.GreaterThan(FixedPoint2.Zero),
            "Mosin was fired but urist sustained no damage!");
    }
}
