using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage.Components;
using Content.Shared.DeadSpace.Player;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Numerics;

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
        Assert.That(damageComp.TotalDamage.Value,
            Is.EqualTo(0),
            "Urist took damage when the weapon should not have fired!");

        await UseInHand();

        Assert.That(wieldComp.Wielded, Is.True, "Mosin failed to wield when interacted with!");

        await AttemptShoot(urist);
        updatedAmmo = gunSystem.GetAmmoCount(mosinEnt);

        Assert.That(updatedAmmo, Is.EqualTo(startAmmo - 1), "Mosin failed to discharge appropriate amount of ammo!");
        Assert.That(damageComp.TotalDamage.Value,
            Is.GreaterThan(0),
            "Mosin was fired but urist sustained no damage!");
    }

    // DS14-start
    [Test]
    public async Task WeaponEffectPvsIncludesRemoteViewSubscribersTest()
    {
        var origin = new MapCoordinates(new Vector2(1000f, 1000f), MapId);
        EntityUid remoteView = default;
        EntityUid projectile = default;
        NetEntity projectileNet = default;
        var config = Server.ResolveDependency<IConfigurationManager>();
        var previousPvs = config.GetCVar(CVars.NetPVS);

        await Server.WaitPost(() => config.SetCVar(CVars.NetPVS, true));

        try
        {
            await Server.WaitPost(() =>
            {
                remoteView = SEntMan.SpawnEntity(null, origin);
                var viewSystem = SEntMan.System<SharedViewSubscriberSystem>();
                viewSystem.AddViewSubscriber(remoteView, ServerSession);

                var attachedOnly = Filter.Empty().AddPlayersByPvs(origin, entManager: SEntMan);
                Assert.That(attachedOnly.Recipients, Does.Not.Contain(ServerSession));

                attachedOnly.AddPlayersByViewSubscriptions(origin, entityManager: SEntMan);
                Assert.That(attachedOnly.Recipients, Does.Contain(ServerSession));

                projectile = SEntMan.SpawnEntity("BulletPistol", origin);
                projectileNet = SEntMan.GetNetEntity(projectile);
            });

            await RunTicks(10);

            await Client.WaitAssertion(() =>
            {
                Assert.That(CEntMan.TryGetEntity(projectileNet, out _), Is.True,
                    "Physical projectiles should already be replicated through subscribed PVS views.");
            });
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                if (remoteView.IsValid() && SEntMan.EntityExists(remoteView))
                    SEntMan.System<SharedViewSubscriberSystem>().RemoveViewSubscriber(remoteView, ServerSession);

                if (projectile.IsValid() && SEntMan.EntityExists(projectile))
                    SEntMan.DeleteEntity(projectile);

                if (remoteView.IsValid() && SEntMan.EntityExists(remoteView))
                    SEntMan.DeleteEntity(remoteView);

                config.SetCVar(CVars.NetPVS, previousPvs);
            });
        }
    }
    // DS14-end
}
