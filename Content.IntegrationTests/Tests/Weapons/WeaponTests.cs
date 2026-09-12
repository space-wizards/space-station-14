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
    private static readonly EntProtoId PistolMk58 = "WeaponPistolMk58";

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

        await AttemptShoot(urist);
        updatedAmmo = gunSystem.GetAmmoCount(mosinEnt);

        Assert.That(updatedAmmo, Is.EqualTo(startAmmo - 1), "Mosin failed to discharge appropriate amount of ammo!");
        Assert.That(damageSystem.GetTotalDamage(ToServer(urist)),
            Is.GreaterThan(FixedPoint2.Zero),
            "Mosin was fired but urist sustained no damage!");
    }

    [Test]
    public async Task SpentCartridgeDoesNotCycleTest()
    {
        var gunSystem = SEntMan.System<SharedGunSystem>();

        await AddAtmosphere(); // prevent the Urist from suffocating

        var urist = await SpawnTarget(MobHuman);

        var pistolNet = await PlaceInHands(PistolMk58);
        var pistolEnt = ToServer(pistolNet);

        await Pair.RunSeconds(2f); // Guns have a cooldown when picking them up.

        await UseInHand(); // Rack the pistol to chamber a round.

        var chambered = gunSystem.GetChamberEntity(pistolEnt);
        Assert.That(chambered, Is.Not.Null, "Pistol has nothing chambered after racking!");

        var cartridge = SEntMan.GetComponent<CartridgeAmmoComponent>(chambered.Value);
        await Server.WaitPost(() =>
        {
            cartridge.Spent = true;
            SEntMan.Dirty(chambered.Value, cartridge);
        });

        var startAmmo = gunSystem.GetAmmoCount(pistolEnt);

        await AttemptShoot(urist);

        Assert.That(gunSystem.GetChamberEntity(pistolEnt),
            Is.EqualTo(chambered),
            "Spent cartridge was cycled out of the chamber!");
        Assert.That(gunSystem.GetAmmoCount(pistolEnt),
            Is.EqualTo(startAmmo),
            "Spent cartridge pulled a fresh round from the magazine!");

        await UseInHand(); // Racking should still clear the dud.

        Assert.That(gunSystem.GetChamberEntity(pistolEnt),
            Is.Not.EqualTo(chambered),
            "Racking did not eject the spent cartridge!");
    }
}
