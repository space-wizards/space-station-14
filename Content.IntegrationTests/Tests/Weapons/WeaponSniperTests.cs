using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Weapons;

public sealed class WeaponSniperTests : InteractionTest
{
    private static readonly EntProtoId MobHuman = "MobHuman";
    private static readonly EntProtoId SniperMosin = "WeaponSniperMosin";

    [Test]
    public async Task SniperMosinInteractionTest()
    {
        var urist = await SpawnTarget(MobHuman);
        var damageComp = Comp<DamageableComponent>(urist);
        var startDamage = damageComp.TotalDamage.Value;

        var mosinNet = await PlaceInHands(SniperMosin);
        var mosinEnt = SEntMan.GetEntity(mosinNet);

        await Pair.RunSeconds(2f); // Something about a cooldown or something IDK

        Assert.That(HasComp<GunRequiresWieldComponent>(mosinNet),
            "Looks like you've removed the 'GunRequiresWield' component from the mosin sniper." +
            "If this was intentional, please update WeaponSniperTests.cs to reflect this change!");

        var ammoEv = new GetAmmoCountEvent();
        SEntMan.EventBus.RaiseLocalEvent(mosinEnt, ref ammoEv);
        var startAmmo = ammoEv.Count;
        var wieldComp = Comp<WieldableComponent>(mosinNet);

        Assert.That(startAmmo, Is.GreaterThan(0), "Mosin was spawned with no ammo!");
        Assert.That(wieldComp.Wielded, Is.False, "Mosin was spawned wielded!");

        await AttemptShoot(urist, false);
        SEntMan.EventBus.RaiseLocalEvent(mosinEnt, ref ammoEv);

        Assert.That(ammoEv.Count,
            Is.EqualTo(startAmmo),
            "Mosin discharged ammo when the weapon should not have fired!");
        Assert.That(damageComp.TotalDamage.Value,
            Is.GreaterThan(startDamage),
            "Urist took damage when the weapon should not have fired!"); 

        await UseInHand();

        Assert.That(wieldComp.Wielded, Is.True, "Mosin failed to wield when interacted with!");

        await AttemptShoot(urist);
        SEntMan.EventBus.RaiseLocalEvent(mosinEnt, ref ammoEv);

        Assert.That(ammoEv.Count, Is.EqualTo(startAmmo - 1), "Mosin failed to discharge appropriate amount of ammo!");
        Assert.That(damageComp.TotalDamage.Value,
            Is.GreaterThan(startDamage),
            "Mosin was fired but urist sustained no damage!");
    }
}
