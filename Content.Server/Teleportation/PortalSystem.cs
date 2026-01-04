using Content.Server.NPC.Pathfinding;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Map;

namespace Content.Server.Teleportation;

public sealed class PortalSystem : SharedPortalSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PathfindingSystem _pathfinding = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PortalComponent, EntityLinkedEvent>(OnLinked);
        SubscribeLocalEvent<PortalComponent, EntityUnlinkedEvent>(OnUnlinked);
    }

    private void OnLinked(Entity<PortalComponent> ent, ref EntityLinkedEvent args)
    {
        if (!ent.Comp.NavPortal)
            return;

        // Only create pathfinding portal once, from the entity with lower UID
        if (ent.Owner.Id > args.Other.Id)
            return;

        var xformA = Transform(ent);
        var xformB = Transform(args.Other);

        if (_pathfinding.TryCreatePortal(xformA.Coordinates, xformB.Coordinates, out var newHandle))
            ent.Comp.NavPortalHandles.Add(args.Other, newHandle);
    }

    private void OnUnlinked(Entity<PortalComponent> ent, ref EntityUnlinkedEvent args)
    {
        if (!ent.Comp.NavPortalHandles.TryGetValue(args.Other, out var handle))
            return;

        _pathfinding.RemovePortal(handle);
        ent.Comp.NavPortalHandles.Remove(args.Other);
    }

    // TODO Move to shared
    protected override void LogTeleport(EntityUid portal, EntityUid subject, EntityCoordinates source,
        EntityCoordinates target)
    {
        if (HasComp<MindContainerComponent>(subject) && !HasComp<GhostComponent>(subject))
            _adminLogger.Add(LogType.Teleport, LogImpact.Low, $"{ToPrettyString(subject):player} teleported via {ToPrettyString(portal)} from {source} to {target}");
    }
}
