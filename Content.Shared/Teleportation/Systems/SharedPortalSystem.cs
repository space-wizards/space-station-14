using System.Linq;
using Content.Shared.Popups;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Triggers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// Resolves linked or random portal destinations and delegates movement to <see cref="SharedTeleportSystem"/>.
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

    private const int MaxRandomTeleportAttempts = 20;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PortalComponent, TeleportUseAttemptEvent>(OnTeleportUseAttempt);
        SubscribeLocalEvent<PortalComponent, TeleportRequestEvent>(OnTeleportRequest);
        SubscribeLocalEvent<PortalComponent, TeleportTriggerExitedEvent>(OnTeleportTriggerExited);
    }

    private void OnTeleportUseAttempt(Entity<PortalComponent> ent, ref TeleportUseAttemptEvent args)
    {
        if (HasComp<PortalTimeoutComponent>(args.Target))
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.RandomTeleport)
            return;

        if (TryComp<LinkedEntityComponent>(ent, out var link) && link.LinkedEntities.Count != 0)
            return;

        args.Cancelled = true;
    }

    private void OnTeleportTriggerExited(Entity<PortalComponent> ent, ref TeleportTriggerExitedEvent args)
    {
        if (TryComp<PortalTimeoutComponent>(args.Target, out var timeout) && timeout.EnteredPortal != ent)
            RemCompDeferred<PortalTimeoutComponent>(args.Target);
    }

    private void OnTeleportRequest(Entity<PortalComponent> ent, ref TeleportRequestEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<LinkedEntityComponent>(ent, out var link) && link.LinkedEntities.Count != 0)
        {
            args.Handled = TryTeleportLinked(ent, link, args.Target, args.TriggerEffects);
            return;
        }

        if (_net.IsClient)
            return;

        if (!ent.Comp.RandomTeleport)
            return;

        var randomDestination = FindRandomDestination(ent);
        if (TryTeleport(ent, args.Target, randomDestination, args.TriggerEffects))
            args.Handled = true;
    }

    private bool TryTeleportLinked(
        Entity<PortalComponent> ent,
        LinkedEntityComponent link,
        EntityUid target,
        bool triggerEffects)
    {
        if (_net.IsClient && !CanPredictTeleport((ent, link)))
            return false;

        var destinationEntity = _random.Pick(link.LinkedEntities);
        if (!Exists(destinationEntity) || TerminatingOrDeleted(destinationEntity))
            return false;

        var destination = Transform(destinationEntity).Coordinates;
        return TryTeleport(ent, target, destination, triggerEffects, destinationEntity);
    }

    private bool TryTeleport(
        Entity<PortalComponent> ent,
        EntityUid target,
        EntityCoordinates destination,
        bool triggerEffects,
        EntityUid? destinationEntity = null)
    {
        if (!TryValidateDestination(ent, destination, destinationEntity))
            return false;

        var addedTimeout = AddTimeout(ent, target, destinationEntity);
        var source = Transform(target).Coordinates;
        if (_teleport.TryTeleport(ent, target, destination, triggerEffects))
        {
            LogTeleport(ent, target, source, destination);
            return true;
        }

        if (addedTimeout)
            RemComp<PortalTimeoutComponent>(target);

        return false;
    }

    private bool AddTimeout(EntityUid portal, EntityUid target, EntityUid? destination)
    {
        // Only collision-triggered return trips need a timeout. Other targets may never raise an exit event.
        if (destination is not { } exit || !HasComp<PortalComponent>(exit) || !_collisionTrigger.CanTrigger(exit, target))
            return false;

        var addedTimeout = !HasComp<PortalTimeoutComponent>(target);
        var timeout = EnsureComp<PortalTimeoutComponent>(target);
        timeout.EnteredPortal = portal;
        Dirty(target, timeout);
        return addedTimeout;
    }

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
