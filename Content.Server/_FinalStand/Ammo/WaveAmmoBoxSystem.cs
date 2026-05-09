using Content.Server._FinalStand.GameTicking.Rules;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Server._FinalStand.Ammo;

public sealed class WaveAmmoBoxSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WaveAmmoBoxComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<WaveAmmoBoxComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<WaveEndedEvent>(OnWaveEnded);
    }

    private void OnPowerChanged(EntityUid uid, WaveAmmoBoxComponent comp, ref PowerChangedEvent args)
    {
        comp.Enabled = args.Powered;
    }

    private void OnWaveEnded(ref WaveEndedEvent ev)
    {
        var query = EntityQueryEnumerator<WaveAmmoBoxComponent>();
        while (query.MoveNext(out _, out var comp))
            comp.UsedBy.Clear();
    }

    private void OnInteractHand(EntityUid uid, WaveAmmoBoxComponent comp, InteractHandEvent args)
    {
        var user = args.User;

        if (!comp.Enabled)
        {
            _popup.PopupEntity(Loc.GetString("wave-ammo-box-unpowered"), uid, user);
            return;
        }

        if (comp.UsedBy.Contains(user))
        {
            _popup.PopupEntity(Loc.GetString("wave-ammo-box-already-used"), uid, user);
            return;
        }

        comp.UsedBy.Add(user);
        RefillAllAmmo(user);
        _popup.PopupEntity(Loc.GetString("wave-ammo-box-used"), uid, user);
    }

    private void RefillAllAmmo(EntityUid player)
    {
        if (!TryComp<ContainerManagerComponent>(player, out var playerMgr))
            return;

        foreach (var container in playerMgr.Containers.Values)
        {
            foreach (var item in container.ContainedEntities)
            {
                // integrated ammo (revolver, etc.)
                TryRefill(item);

                // explicitly refill gun magazine slots by known slot names
                TryRefillGunMag(item);

                // general one-level-deep search (backpack contents, holstered gun mags, etc.)
                if (!TryComp<ContainerManagerComponent>(item, out var itemMgr))
                    continue;
                foreach (var inner in itemMgr.Containers.Values)
                {
                    foreach (var nested in inner.ContainedEntities)
                    {
                        TryRefill(nested);
                        TryRefillGunMag(nested);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handles guns with detachable magazines by looking up the "gun_magazine" slot directly.
    /// This covers MagazineAmmoProviderComponent and ChamberMagazineAmmoProviderComponent guns.
    /// </summary>
    private void TryRefillGunMag(EntityUid gun)
    {
        if (_containers.TryGetContainer(gun, SharedGunSystem.MagazineSlot, out var magSlot))
        {
            foreach (var mag in magSlot.ContainedEntities)
                TryRefill(mag);
        }
    }

    private void TryRefill(EntityUid entity)
    {
        if (!TryComp<BallisticAmmoProviderComponent>(entity, out var ballistic))
            return;
        var spawned = ballistic.Count - ballistic.UnspawnedCount;
        var target = ballistic.Capacity - spawned;
        if (target <= ballistic.UnspawnedCount)
            return;
        _gun.SetBallisticUnspawned((entity, ballistic), target);
    }
}
