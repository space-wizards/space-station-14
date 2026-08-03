// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Collections.Generic;
using System.Numerics;
using Content.Server.DeadSpace.Lavaland.Components;
using Content.Server.DeadSpace.Xenoborgs.Components;
using Content.Server.Tiles;
using Content.Shared.Chasm;
using Content.Shared.DeadSpace.Xenoborgs.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Xenoborgs.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Xenoborgs;

public sealed class XenoborgTeleportationSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoborgPortalGunComponent, AmmoShotEvent>(OnAmmoShot);
        SubscribeLocalEvent<XenoborgPortalProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<XenoborgJaunterComponent, UseInHandEvent>(OnJaunterUsed);
        SubscribeLocalEvent<XenoborgMinerEquipmentModuleComponent,
            BorgModuleRelayedEvent<BorgModuleInsertAttemptEvent>>(OnMinerModuleInsertAttempt);
    }

    private void OnMinerModuleInsertAttempt(
        Entity<XenoborgMinerEquipmentModuleComponent> ent,
        ref BorgModuleRelayedEvent<BorgModuleInsertAttemptEvent> args)
    {
        if (!HasComp<XenoborgMinerEquipmentModuleComponent>(args.Args.ModuleEnt))
            return;

        args.Args.Cancelled = true;
        args.Args.Reason = Loc.GetString("xenoborg-miner-module-exclusive", ("existing", ent.Owner));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<XenoborgPortalGunComponent>();
        while (query.MoveNext(out var uid, out var gun))
        {
            if (gun.PendingProjectile == null || _timing.CurTime < gun.PendingUntil)
                continue;

            var projectile = gun.PendingProjectile.Value;
            ResolveProjectileCooldown((uid, gun), projectile, gun.MissCooldown);
            if (Exists(projectile))
                QueueDel(projectile);
        }
    }

    private void OnAmmoShot(Entity<XenoborgPortalGunComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            if (!TryComp<XenoborgPortalProjectileComponent>(projectile, out var portalProjectile))
                continue;

            portalProjectile.Gun = ent.Owner;
            ent.Comp.PendingProjectile = projectile;
            ent.Comp.ShotTime = _timing.CurTime;
            ent.Comp.PendingUntil = _timing.CurTime + ent.Comp.MissCooldown;

            _useDelay.SetLength(ent.Owner, ent.Comp.MissCooldown);
            _useDelay.TryResetDelay(ent.Owner);
            break;
        }
    }

    private void OnProjectileHit(Entity<XenoborgPortalProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (ent.Comp.Gun is not { } gunUid ||
            !TryComp<XenoborgPortalGunComponent>(gunUid, out var gun) ||
            gun.PendingProjectile != ent.Owner)
        {
            QueueDel(ent.Owner);
            return;
        }

        var cooldown = gun.MissCooldown;
        if (TryComp<MobStateComponent>(args.Target, out var mobState) &&
            !_mobState.IsDead(args.Target, mobState) &&
            !HasComp<LavalandBossComponent>(args.Target) &&
            TryFindSafeTileOnCurrentGrid(args.Target, out var destination))
        {
            cooldown = HasComp<StunnedComponent>(args.Target)
                ? gun.StunnedCooldown
                : gun.UnstunnedCooldown;

            StopPulling(args.Target);
            _transform.SetCoordinates(args.Target, destination);
        }

        ResolveProjectileCooldown((gunUid, gun), ent.Owner, cooldown);
        QueueDel(ent.Owner);
    }

    private void ResolveProjectileCooldown(
        Entity<XenoborgPortalGunComponent> gun,
        EntityUid projectile,
        TimeSpan cooldown)
    {
        if (gun.Comp.PendingProjectile != projectile)
            return;

        gun.Comp.PendingProjectile = null;
        var remaining = cooldown - (_timing.CurTime - gun.Comp.ShotTime);
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        _useDelay.SetLength(gun.Owner, remaining);
        _useDelay.TryResetDelay(gun.Owner);
    }

    private void OnJaunterUsed(Entity<XenoborgJaunterComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.ApplyDelay = false;

        if (TryComp<UseDelayComponent>(ent, out var delay) && _useDelay.IsDelayed((ent.Owner, delay)))
            return;

        if (!TryFindJaunterDestination(args.User, ent.Comp.SearchRadius, out var destination))
        {
            _popup.PopupEntity(
                Loc.GetString("xenoborg-jaunter-no-destination"),
                args.User,
                args.User,
                PopupType.MediumCaution);
            return;
        }

        StopPulling(args.User);
        _transform.SetCoordinates(args.User, destination);
        _useDelay.TryResetDelay(ent.Owner);
        _popup.PopupEntity(
            Loc.GetString("xenoborg-jaunter-activate"),
            args.User,
            args.User,
            PopupType.Medium);
    }

    private bool TryFindSafeTileOnCurrentGrid(EntityUid target, out EntityCoordinates destination)
    {
        destination = default;
        var xform = Transform(target);
        if (xform.GridUid is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var grid) ||
            !_turf.TryGetTileRef(xform.Coordinates, out var sourceTile))
        {
            return false;
        }

        return TryPickSafeTile(
            gridUid,
            grid,
            target,
            sourceTile.Value.GridIndices,
            null,
            null,
            out destination);
    }

    private bool TryFindJaunterDestination(EntityUid user, float radius, out EntityCoordinates destination)
    {
        destination = default;
        var query = EntityQueryEnumerator<MothershipCoreComponent, TransformComponent>();
        while (query.MoveNext(out var coreUid, out _, out var coreXform))
        {
            if (TerminatingOrDeleted(coreUid) ||
                EntityManager.IsQueuedForDeletion(coreUid) ||
                coreXform.GridUid is not { } gridUid ||
                !TryComp<MapGridComponent>(gridUid, out var grid))
            {
                continue;
            }

            var center = _transform.GetMapCoordinates(coreUid).Position;
            if (TryPickSafeTile(
                    gridUid,
                    grid,
                    user,
                    null,
                    center,
                    radius,
                    out destination))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryPickSafeTile(
        EntityUid gridUid,
        MapGridComponent grid,
        EntityUid subject,
        Vector2i? excludedTile,
        Vector2? center,
        float? radius,
        out EntityCoordinates destination)
    {
        destination = default;
        var collisionMask = TryComp<PhysicsComponent>(subject, out var physics)
            ? (CollisionGroup) physics.CollisionMask
            : CollisionGroup.MobMask;

        var count = 0;

        IEnumerable<TileRef> tiles = center is { } centerValue && radius is { } radiusValue
            ? _map.GetTilesIntersecting(
                gridUid,
                grid,
                new Circle(centerValue, radiusValue),
                ignoreEmpty: false)
            : _map.GetAllTiles(gridUid, grid);

        foreach (var tile in tiles)
        {
            if (!IsSafeTile(tile, grid, excludedTile, collisionMask))
                continue;

            count++;
            if (_random.Next(count) == 0)
                destination = _map.ToCenterCoordinates(tile);
        }

        return count > 0;
    }

    private bool IsSafeTile(
        TileRef tile,
        MapGridComponent grid,
        Vector2i? excludedTile,
        CollisionGroup collisionMask)
    {
        return excludedTile != tile.GridIndices &&
               !tile.Tile.IsEmpty &&
               !_turf.IsSpace(tile) &&
               !_turf.IsTileBlocked(tile, collisionMask) &&
               !HasUnsafeAnchoredEntity(tile, grid);
    }

    private bool HasUnsafeAnchoredEntity(TileRef tile, MapGridComponent grid)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(tile.GridUid, grid, tile.GridIndices);
        while (anchored.MoveNext(out var anchoredUid))
        {
            if (anchoredUid == null || TerminatingOrDeleted(anchoredUid.Value))
                continue;

            if (HasComp<ChasmComponent>(anchoredUid.Value) ||
                HasComp<TileEntityEffectComponent>(anchoredUid.Value))
            {
                return true;
            }
        }

        return false;
    }

    private void StopPulling(EntityUid target)
    {
        if (TryComp<PullableComponent>(target, out var pullable) &&
            _pulling.IsPulled(target, pullable))
        {
            _pulling.TryStopPull(target, pullable);
        }

        if (TryComp<PullerComponent>(target, out var puller) &&
            puller.Pulling is { } pulled &&
            TryComp<PullableComponent>(pulled, out var pulledComp))
        {
            _pulling.TryStopPull(pulled, pulledComp);
        }
    }
}
