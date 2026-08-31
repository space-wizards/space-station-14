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

    [SidedDependency(Side.Server)] private DamageableSystem _sDamageable = default!;
    [SidedDependency(Side.Server)] private SharedGunSystem _sGun = default!;

    [Test]
    [Description("Tests that a gun that needs wielding can be spawned and shot properly.")]
    public async Task GunRequiresWieldTest()
    {
        await AddAtmosphere(); // prevent the Urist from suffocating

        var uristNet = await SpawnTarget(MobHuman);
        Entity<DamageableComponent> uristEnt = (ToServer(uristNet), Comp<DamageableComponent>(uristNet));

        var mosinNet = await PlaceInHands(SniperMosin);
        var mosinEnt = ToServer(mosinNet);

        await Pair.RunSeconds(2f); // Guns have a cooldown when picking them up.

        Assert.That(HasComp<GunRequiresWieldComponent>(mosinNet),
            "Looks like you've removed the 'GunRequiresWield' component from the mosin sniper." +
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
        Assert.That(_sDamageable.GetPositiveDamage(uristEnt).GetTotal(),
            Is.EqualTo(FixedPoint2.Zero),
            "Urist took damage when the weapon should not have fired!");

        await UseInHand();

        Assert.That(wieldComp.Wielded, Is.True, "Mosin did not wield upon interaction!");

        await AttemptShoot(uristNet);
        updatedAmmo = _sGun.GetAmmoCount(mosinEnt);

        Assert.That(updatedAmmo, Is.EqualTo(startAmmo - 1), "Mosin failed to discharge appropriate amount of ammo!");
        Assert.That(_sDamageable.GetPositiveDamage(uristEnt).GetTotal(),
            Is.GreaterThan(FixedPoint2.Zero),
            "Mosin was fired but urist sustained no damage!");
    }
}
