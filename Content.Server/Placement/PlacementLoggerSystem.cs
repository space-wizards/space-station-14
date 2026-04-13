using System.Text.Json;
using Content.Server.Administration.AuditLog;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.Map;
using Robust.Shared.Placement;
using Robust.Shared.Player;

namespace Content.Server.Placement;

public sealed class PlacementLoggerSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAdminAuditLogManager _auditLog = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlacementEntityEvent>(OnEntityPlacement);
        SubscribeLocalEvent<PlacementTileEvent>(OnTilePlacement);
    }

    private void OnEntityPlacement(PlacementEntityEvent ev)
    {
        _player.TryGetSessionById(ev.PlacerNetUserId, out var actor);
        var actorEntity = actor?.AttachedEntity;

        var logType = ev.PlacementEventAction switch
        {
            PlacementEventAction.Create => LogType.EntitySpawn,
            PlacementEventAction.Erase => LogType.EntityDelete,
            _ => LogType.Action
        };

        if (actor is { } actorSession && actorEntity != null)
        {
            _adminLogger.Add(
                logType,
                LogImpact.Medium,
                $"{actorEntity.Value:actor} used placement system to {ev.PlacementEventAction.ToString().ToLower()} {ev.EditedEntity:subject} at {ev.Coordinates}",
                new
                {
                    action = ev.PlacementEventAction.ToString(),
                    coordinates = ev.Coordinates.ToString()
                });

            if (_adminManager.IsAdmin(actorSession, includeDeAdmin: true) &&
                (ev.PlacementEventAction == PlacementEventAction.Create || ev.PlacementEventAction == PlacementEventAction.Erase))
            {
                var action = ev.PlacementEventAction == PlacementEventAction.Create
                    ? AdminAuditAction.SpawnEntity
                    : AdminAuditAction.DeleteEntity;

                _auditLog.LogAction(
                    actorSession.UserId.UserId,
                    action,
                    AuditSeverity.Notable,
                    $"Placement system {ev.PlacementEventAction.ToString().ToLower()}d entity {ToPrettyString(ev.EditedEntity)} at {ev.Coordinates}",
                    targetEntity: ev.EditedEntity,
                    payload: JsonSerializer.SerializeToDocument(new
                    {
                        action = ev.PlacementEventAction.ToString(),
                        editedEntity = (int) ev.EditedEntity,
                        coordinates = ev.Coordinates.ToString()
                    }));
            }
        }
        else if (actor != null)
            _adminLogger.Add(logType, LogImpact.Medium,
                $"{actor:actor} used placement system to {ev.PlacementEventAction.ToString().ToLower()} {ev.EditedEntity:subject} at {ev.Coordinates}");
        else
            _adminLogger.Add(logType, LogImpact.Medium,
                $"Placement system {ev.PlacementEventAction.ToString().ToLower()}ed {ev.EditedEntity:subject} at {ev.Coordinates}");
    }

    private void OnTilePlacement(PlacementTileEvent ev)
    {
        _player.TryGetSessionById(ev.PlacerNetUserId, out var actor);
        var actorEntity = actor?.AttachedEntity;
        var tileName = _tileDefinitionManager[ev.TileType].Name;

        if (actorEntity != null)
        {
            _adminLogger.Add(
                LogType.Tile,
                LogImpact.Medium,
                $"{actorEntity.Value:actor} used placement system to set tile {tileName} at {ev.Coordinates}",
                new
                {
                    tile = tileName,
                    coordinates = ev.Coordinates.ToString()
                });
        }
        else if (actor != null)
        {
            _adminLogger.Add(LogType.Tile, LogImpact.Medium,
                $"{actor:player} used placement system to set tile {tileName} at {ev.Coordinates}");
        }
        else
        {
            _adminLogger.Add(LogType.Tile, LogImpact.Medium,
                $"Placement system set tile {tileName} at {ev.Coordinates}");
        }
    }
}
