using System.Runtime.InteropServices;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Logs.Converters;
using Content.Shared.Database;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Server.Placement;

public sealed class PlacementLoggerSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlacementEntityEvent>(OnEntityPlacement);
        SubscribeLocalEvent<PlacementTileEvent>(OnTilePlacement);
    }

    private (IPlayerSession? actor, EntityUid? actorEntity) GetActor(NetUserId? userId)
    {
        IPlayerSession? actor = null;
        EntityUid? actorEntity = null;
        if (userId != null)
        {
            actor = _playerManager.GetSessionByUserId(userId.Value);
            actorEntity = actor.AttachedEntity;
        }

        return (actor, actorEntity);
    }

    private void OnEntityPlacement(PlacementEntityEvent ev)
    {
        var (actor, actorEntity) = GetActor(ev.PlacerNetUserId);

        var logType = ev.PlacementEventAction switch
        {
            PlacementEventAction.Create => LogType.EntitySpawn,
            PlacementEventAction.Erase => LogType.EntityDelete,
            _ => LogType.Action
        };

        if (actorEntity != null)
            _adminLogger.Add(logType, LogImpact.High,
                $"{ToPrettyString(actorEntity.Value):actor} used placement system to {ev.PlacementEventAction.ToString().ToLower()} {ToPrettyString(ev.EditedEntity):subject} at {ev.Coordinates}");
        else if (actor != null)
            _adminLogger.Add(logType, LogImpact.High,
                $"{actor:actor} used placement system to {ev.PlacementEventAction.ToString().ToLower()} {ToPrettyString(ev.EditedEntity):subject} at {ev.Coordinates}");
        else
            _adminLogger.Add(logType, LogImpact.High,
                $"Placement system {ev.PlacementEventAction.ToString().ToLower()}ed {ToPrettyString(ev.EditedEntity):subject} at {ev.Coordinates}");
    }

    private void OnTilePlacement(PlacementTileEvent ev)
    {
        var (actor, actorEntity) = GetActor(ev.PlacerNetUserId);

        if (actorEntity != null)
            _adminLogger.Add(LogType.Action, LogImpact.High,
                $"{ToPrettyString(actorEntity.Value):actor} used placement system to set tile {_tileDefinitionManager[ev.TileType].Name} at {ev.Coordinates}");
        else if (actor != null)
            _adminLogger.Add(LogType.Action, LogImpact.High,
                $"{actor} used placement system to set tile {_tileDefinitionManager[ev.TileType].Name} at {ev.Coordinates}");
        else
            _adminLogger.Add(LogType.Action, LogImpact.High,
                $"Placement system set tile {_tileDefinitionManager[ev.TileType].Name} at {ev.Coordinates}");
    }
}
