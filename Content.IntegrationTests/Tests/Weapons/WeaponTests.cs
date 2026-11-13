#nullable enable
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Weapons;

public sealed class WeaponTests : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman"; // The default test mob only has one hand
    private static readonly EntProtoId MobHuman = "MobHuman";
    private static readonly EntProtoId SniperMosin = "WeaponSniperMosin";
    private static readonly EntProtoId PistolMk58 = "WeaponPistolMk58";

    [SidedDependency(Side.Server)] private DamageableSystem _sDamageable = default!;
    [SidedDependency(Side.Server)] private SharedGunSystem _sGun = default!;

    [Test]
    [Description("Tests that a gun that needs wielding can be spawned and shot properly.")]
    public async Task GunRequiresWieldTest()
    {
        await AddAtmosphere(); // prevent the Urist from suffocating

        var uristNet = await SpawnTarget(MobHuman);
        Entity<DamageableComponent> sUrist = (ToServer(uristNet), Comp<DamageableComponent>(uristNet));

        var mosinNet = await PlaceInHands(SniperMosin);
        var mosinEnt = ToServer(mosinNet);

        await Pair.RunSeconds(2f); // Guns have a cooldown when picking them up.

        Assert.That(HasComp<GunRequiresWieldComponent>(mosinNet),
            $"Looks like you've removed the '{nameof(GunRequiresWieldComponent)}' from the mosin sniper." +
            "If this was intentional, please update WeaponTests.cs to reflect this change!");

        var startAmmo = _sGun.GetAmmoCount(mosinEnt);
        var wieldComp = Comp<WieldableComponent>(mosinNet);

        Assert.That(startAmmo, Is.GreaterThan(0), "Mosin was spawned with no ammo!");
        Assert.That(wieldComp.Wielded, Is.False, "Mosin was spawned wielded!");

        await AttemptShoot(uristNet, false); // should fail due to not being wielded
        var updatedAmmo = _sGun.GetAmmoCount(mosinEnt);

        Assert.That(updatedAmmo,
            Is.EqualTo(startAmmo),
            "Mosin discharged ammo when the weapon should not have fired!");
        Assert.That(_sDamageable.GetPositiveDamage(sUrist).GetTotal(),
            Is.EqualTo(FixedPoint2.Zero),
            "Urist took damage when the weapon should not have fired!");

        await UseInHand();

        Assert.That(wieldComp.Wielded, Is.True, "Mosin did not wield upon interaction!");

        await AttemptShoot(uristNet);
        updatedAmmo = _sGun.GetAmmoCount(mosinEnt);

        Assert.That(updatedAmmo, Is.EqualTo(startAmmo - 1), "Mosin failed to discharge appropriate amount of ammo!");
        Assert.That(_sDamageable.GetPositiveDamage(sUrist).GetTotal(),
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
