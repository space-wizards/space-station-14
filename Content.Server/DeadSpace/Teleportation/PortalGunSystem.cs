// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Teleportation.Components;
using Content.Shared.Teleportation;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Warps;
using Content.Shared.Trigger;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Spawners;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Trigger.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Teleportation;

public sealed class PortalGunSystem : EntitySystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string PortalTriggerKey = "openportal";

    public override void Initialize()
    {
        SubscribeLocalEvent<PortalGunComponent, TeleportLocationDestinationMessage>(OnDestinationSelected);
        SubscribeLocalEvent<PortalGunComponent, AmmoShotEvent>(OnAmmoShot);
        SubscribeLocalEvent<PortalGunComponent, ShotAttemptedEvent>(OnShotAttempt);
        SubscribeLocalEvent<PortalProjectileComponent, TriggerEvent>(OnTrigger);
    }

    private void OnDestinationSelected(Entity<PortalGunComponent> ent, ref TeleportLocationDestinationMessage args)
    {
        if (!TryGetEntity(args.NetEnt, out var target) || !HasComp<WarpPointComponent>(target))
            return;
        ent.Comp.SelectedDestination = target;
        _popup.PopupEntity(Loc.GetString("portal-gun-destination-set"), args.Actor, args.Actor);
    }

    private void OnAmmoShot(Entity<PortalGunComponent> ent, ref AmmoShotEvent args)
    {
        if (!TryComp<GunComponent>(ent, out var gun) || gun.ShootCoordinates == null)
            return;

        var targetMap = _transform.ToMapCoordinates(gun.ShootCoordinates.Value);

        foreach (var projectile in args.FiredProjectiles)
        {
            if (!TryComp<PortalProjectileComponent>(projectile, out var projComp))
                continue;

            projComp.Destination = ent.Comp.SelectedDestination;
            if (!TryComp<TimerTriggerComponent>(projectile, out var timer))
                continue;

            var projMap = _transform.GetMapCoordinates(projectile);
            if (projMap.MapId != targetMap.MapId)
                continue;

            var distance = (targetMap.Position - projMap.Position).Length();
            var speed = _physics.GetMapLinearVelocity(projectile).Length();
            if (speed <= 0f)
                continue;

            timer.Delay = TimeSpan.FromSeconds(distance / speed);
            timer.NextTrigger = _timing.CurTime + timer.Delay;
            Dirty(projectile, timer);
        }
    }

    private void OnShotAttempt(Entity<PortalGunComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.SelectedDestination != null)
            return;
        _popup.PopupEntity(Loc.GetString("portal-gun-destination-fail"), args.User, args.User);
        args.Cancel();
    }

    private void OnTrigger(Entity<PortalProjectileComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != PortalTriggerKey)
            return;
        if (ent.Comp.Destination == null || TerminatingOrDeleted(ent.Comp.Destination.Value))
            return;

        var xform = Transform(ent.Owner);
        var nearCoords = xform.Coordinates;
        if (_turf.TryGetTileRef(nearCoords, out var tile) && _turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
            nearCoords = nearCoords.Offset(-xform.LocalRotation.ToWorldVec());

        var near = Spawn(ent.Comp.NearPortal, nearCoords);
        var far = Spawn(ent.Comp.FarPortal, Transform(ent.Comp.Destination.Value).Coordinates);
        _link.TryLink(near, far, true);
        EnsureComp<TimedDespawnComponent>(near).Lifetime = (float)ent.Comp.Lifetime.TotalSeconds;
        EnsureComp<TimedDespawnComponent>(far).Lifetime = (float)ent.Comp.Lifetime.TotalSeconds;
        ent.Comp.Destination = null;
        QueueDel(ent.Owner);
    }
}