using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Pointing.Components;
using Content.Shared.Database;
using Content.Shared.Pointing;
using Robust.Shared.Map;

namespace Content.Server.Pointing.EntitySystems;

public sealed partial class PointingSystem : SharedPointingSystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;

    protected override bool ShouldPointingArrowGoRogue()
    {
        return EntityQuery<PointingArrowAngeringComponent>().FirstOrDefault() != null;
    }

    protected override void AfterPoint(
        EntityUid player,
        MapCoordinates mapCoordsPointed,
        EntityUid pointed)
    {
        base.AfterPoint(player, mapCoordsPointed, pointed);

        if (Exists(pointed))
        {
            var effectivePointed = GetPointingTarget(pointed);
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):user} pointed at {ToPrettyString(effectivePointed):target} {Transform(effectivePointed).Coordinates}");
        }
        else
        {
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):user} pointed at {GetTileName(mapCoordsPointed)} {GetTileLogPosition(mapCoordsPointed)}");
        }
    }

}
