using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Placement;

namespace Content.Server.Placement;

public sealed class PlacementLoggerSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly ActorSystem _actorSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlacementEntityEvent>(OnEntityPlacement);
        SubscribeLocalEvent<PlacementTileEvent>(OnTilePlacement);
    }

    private void OnEntityPlacement(PlacementEntityEvent ev)
    {
        _actorSystem.TryGetActorFromUserId(ev.PlacerNetUserId, out var actor, out var actorEntity);

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
        _actorSystem.TryGetActorFromUserId(ev.PlacerNetUserId, out var actor, out var actorEntity);

        if (actorEntity != null)
            _adminLogger.Add(LogType.Tile, LogImpact.High,
                $"{ToPrettyString(actorEntity.Value):actor} used placement system to set tile {_tileDefinitionManager[ev.TileType].Name} at {ev.Coordinates}");
        else if (actor != null)
            _adminLogger.Add(LogType.Tile, LogImpact.High,
                $"{actor} used placement system to set tile {_tileDefinitionManager[ev.TileType].Name} at {ev.Coordinates}");
        else
            _adminLogger.Add(LogType.Tile, LogImpact.High,
                $"Placement system set tile {_tileDefinitionManager[ev.TileType].Name} at {ev.Coordinates}");
    }
}
