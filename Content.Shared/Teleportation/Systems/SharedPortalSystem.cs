using System.Linq;
using Content.Shared.Popups;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Triggers;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// Chooses linked or random portal destinations. <see cref="SharedTeleportSystem"/> teleports the targets.
/// </summary>
/// <seealso cref="PortalComponent"/>
public abstract partial class SharedPortalSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTeleportSystem _teleport = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private CollisionTeleportTriggerSystem _collisionTrigger = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private const int MaxRandomTeleportAttempts = 20;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PortalComponent, TeleportUseAttemptEvent>(OnTeleportUseAttempt);
        SubscribeLocalEvent<PortalComponent, TeleportRequestEvent>(OnTeleportRequest);
        SubscribeLocalEvent<PortalComponent, TeleportTriggerExitedEvent>(OnTeleportTriggerExited);
    }

    /// <summary>
    /// Checks exit protection and, for unlinked portals, user restrictions.
    /// Does not choose a destination or teleport the target.
    /// </summary>
    private void OnTeleportUseAttempt(Entity<PortalComponent> ent, ref TeleportUseAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryComp<PortalTimeoutComponent>(args.Target, out var timeout) && IsTimeoutActive(args.Target, timeout))
        {
            args.Cancel();
            return;
        }

        if (TryComp<LinkedEntityComponent>(ent, out var link) && link.LinkedEntities.Count != 0)
            return;

        if (!ent.Comp.RandomTeleport)
        {
            args.Cancel();
            return;
        }

        if (args.User == null)
            return;

        if (!_whitelist.CheckBoth(
                args.User.Value,
                blacklist: ent.Comp.UnlinkedUserBlacklist,
                whitelist: ent.Comp.UnlinkedUserWhitelist))
        {
            args.Cancel("portal-component-unlinked-user-denied");
        }
    }

    /// <summary>
    /// Rechecks exit protection when a contact with the exit portal ends.
    /// </summary>
    private void OnTeleportTriggerExited(Entity<PortalComponent> ent, ref TeleportTriggerExitedEvent args)
    {
        if (!TryComp<PortalTimeoutComponent>(args.Target, out var timeout))
            return;

        if (timeout.ExitPortal != ent.Owner)
            return;

        RefreshTimeout(args.Target, timeout);
    }

    /// <summary>
    /// Handles a teleport request through a linked exit or, on the server, a random nearby point.
    /// Ignores requests already handled.
    /// </summary>
    private void OnTeleportRequest(Entity<PortalComponent> ent, ref TeleportRequestEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (TryComp<LinkedEntityComponent>(ent, out var link) && link.LinkedEntities.Count != 0)
        {
            args.Succeeded = TryTeleportLinked(ent, link, args.Target, args.TriggerEffects);
            return;
        }

        if (_net.IsClient)
            return;

        if (!ent.Comp.RandomTeleport)
            return;

        var randomDestination = FindRandomDestination(ent);
        args.Succeeded = TryTeleport(ent, args.Target, randomDestination, args.TriggerEffects);
    }

    /// <summary>
    /// Selects a linked destination. Clients run this only when they can predict the destination.
    /// </summary>
    private bool TryTeleportLinked(
        Entity<PortalComponent> ent,
        LinkedEntityComponent link,
        EntityUid target,
        bool triggerEffects)
    {
        if (_net.IsClient && !CanPredictTeleport((ent, link)))
            return false;

        var destinationEntity = _random.Pick(link.LinkedEntities);
        if (TerminatingOrDeleted(destinationEntity))
            return false;

        var destination = Transform(destinationEntity).Coordinates;
        return TryTeleport(ent, target, destination, triggerEffects, destinationEntity);
    }

    /// <summary>
    /// Validates a portal destination and protects the target from returning through the exit.
    /// Restores the previous timeout if the target does not reach the destination,
    /// even if an effect throws before teleportation.
    /// </summary>
    private bool TryTeleport(
        Entity<PortalComponent> ent,
        EntityUid target,
        EntityCoordinates destination,
        bool triggerEffects,
        EntityUid? destinationEntity = null)
    {
        if (!TryValidateDestination(ent, destination, destinationEntity))
            return false;

        if (!TryValidateExit(target, destination, destinationEntity))
            return false;

        var source = Transform(target).Coordinates;
        var previousExit = CompOrNull<PortalTimeoutComponent>(target)?.ExitPortal;
        var timeoutSet = destinationEntity != null && SetPortalTimeout(target, destinationEntity.Value);
        var reachedDestination = false;
        try
        {
            if (!_teleport.TryTeleport(ent, target, destination, out reachedDestination, triggerEffects))
                return false;

            LogTeleport(ent, target, source, destination);
            return true;
        }
        finally
        {
            if (timeoutSet)
                RestoreTimeoutAfterFailedTeleport(target, previousExit, reachedDestination);
        }
    }

    /// <summary>
    /// Rejects linked exits where the target would overlap a static obstacle, showing a popup on failure.
    /// Random destinations use their own candidate selection instead.
    /// </summary>
    private bool TryValidateExit(EntityUid target, EntityCoordinates destination, EntityUid? exit)
    {
        if (exit == null)
            return true;

        var mapCoordinates = _transform.ToMapCoordinates(destination);
        var targetTransform = Transform(target);
        var rotation = _transform.GetWorldRotation(targetTransform);
        // Bodies without local rotation follow the destination parent's rotation after reparenting.
        if (targetTransform.NoLocalRotation)
            rotation = _transform.GetWorldRotation(destination.EntityId);

        if (!_teleport.IsDestinationBlocked(target, mapCoordinates, rotation, LookupFlags.Static))
            return true;

        _popup.PopupEntity(Loc.GetString("portal-component-exit-blocked"), target, target);
        return false;
    }

    /// <summary>
    /// Checks the portal's map and distance limits. If a limit fails, the server shows the fizzle message
    /// and queues the source portal and any linked destination for deletion.
    /// </summary>
    private bool TryValidateDestination(
        Entity<PortalComponent> ent,
        EntityCoordinates destination,
        EntityUid? destinationEntity)
    {
        var source = Transform(ent).Coordinates;
        var onSameMap = _transform.GetMapId(source) == _transform.GetMapId(destination);
        var mapInvalid = !onSameMap && !ent.Comp.CanTeleportToOtherMaps;
        var distanceInvalid = ent.Comp.MaxTeleportRadius != null
                              && source.TryDistance(EntityManager, destination, out var distance)
                              && distance > ent.Comp.MaxTeleportRadius;

        if (!mapInvalid && !distanceInvalid)
            return true;

        if (_net.IsClient)
            return false;

        _popup.PopupCoordinates(
            Loc.GetString("portal-component-invalid-configuration-fizzle"),
            source,
            Filter.Pvs(source, entityMan: EntityManager),
            true);

        _popup.PopupCoordinates(
            Loc.GetString("portal-component-invalid-configuration-fizzle"),
            destination,
            Filter.Pvs(destination, entityMan: EntityManager),
            true);

        QueueDel(ent);

        if (destinationEntity != null)
            QueueDel(destinationEntity.Value);

        return false;
    }

    /// <summary>
    /// Tries to choose a nearby point clear of static entities.
    /// Falls back to the final candidate after the attempt limit, even if that point is occupied.
    /// </summary>
    private EntityCoordinates FindRandomDestination(Entity<PortalComponent> ent)
    {
        var source = Transform(ent).Coordinates;
        var destination = source.Offset(_random.NextVector2(ent.Comp.MaxRandomRadius));

        for (var i = 0; i < MaxRandomTeleportAttempts; i++)
        {
            destination = source.Offset(_random.NextVector2(ent.Comp.MaxRandomRadius));
            if (!_lookup.AnyEntitiesIntersecting(_transform.ToMapCoordinates(destination), LookupFlags.Static))
                break;
        }

        // If all attempts fail, use the last candidate even if it places the target inside a wall.
        return destination;
    }

    /// <summary>
    /// Logs a successful portal teleport on the server.
    /// </summary>
    protected virtual void LogTeleport(
        EntityUid portal,
        EntityUid target,
        EntityCoordinates source,
        EntityCoordinates destination)
    {
    }

    /// <summary>
    /// Clients can predict teleportation only with exactly one linked exit, available locally and outside nullspace.
    /// Multiple exits require a random choice by the server.
    /// </summary>
    private bool CanPredictTeleport(Entity<LinkedEntityComponent> portal)
    {
        if (portal.Comp.LinkedEntities.Count != 1)
            return false;

        var destination = portal.Comp.LinkedEntities.First();
        if (!Exists(destination))
            return false;

        return Transform(destination).MapID != MapId.Nullspace;
    }
}
