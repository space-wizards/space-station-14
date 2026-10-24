using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Map;

namespace Content.Server.Teleportation;

public sealed partial class PortalSystem : SharedPortalSystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;

    // TODO Move to shared
    protected override void LogTeleport(EntityUid portal, EntityUid target, EntityCoordinates source,
        EntityCoordinates destination)
    {
        if (HasComp<MindContainerComponent>(target) && !HasComp<GhostComponent>(target))
            _adminLogger.Add(LogType.Teleport, LogImpact.Low, $"{ToPrettyString(target):player} teleported via {ToPrettyString(portal)} from {source} to {destination}");
    }
}
