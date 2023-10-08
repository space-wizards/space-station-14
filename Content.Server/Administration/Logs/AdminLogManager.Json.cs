using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.Administration.Logs.Converters;
using Content.Server.Database;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Logs;

public sealed partial class AdminLogManager
{
    private static readonly JsonNamingPolicy NamingPolicy = JsonNamingPolicy.CamelCase;

    // Init only
    private JsonSerializerOptions _jsonOptions = default!;

    private void InitializeJson()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = NamingPolicy
        };

        foreach (var converter in _reflection.FindTypesWithAttribute<AdminLogConverterAttribute>())
        {
            var instance = _typeFactory.CreateInstance<JsonConverter>(converter);
            _jsonOptions.Converters.Add(instance);
        }

        var converterNames = _jsonOptions.Converters.Select(converter => converter.GetType().Name);
        _sawmill.Debug($"Admin log converters found: {string.Join(" ", converterNames)}");
    }

    private (JsonDocument Json, List<AdminLogPlayer> Players, List<AdminLogEntity> Entities) ToJson(
        int id,
        Dictionary<string, object?> properties)
    {
        var entities = new List<AdminLogEntity>();
        var players = new List<AdminLogPlayer>();
        var parsed = new Dictionary<string, object?>(properties.Count);

        foreach (var (key, value) in properties)
        {
            var parsedKey = NamingPolicy.ConvertName(key);
            switch (value)
            {
                case EntityUid uid:
                    var entityRep = _entityManager.ToPrettyString(uid);
                    parsed.Add(parsedKey, entityRep);
                    AddEntity(id, entityRep, entities, players);
                    break;
                case EntityStringRepresentation rep:
                    parsed.Add(parsedKey, rep);
                    AddEntity(id, rep, entities, players);
                    break;
                case EntityCoordinates coords:
                    _xformQuery.TryGetComponent(coords.EntityId, out var xform);
                    _metaQuery.TryGetComponent(coords.EntityId, out var metaData);
                    _metaQuery.TryGetComponent(xform?.MapUid, out var mapMeta);
                    parsed.Add(parsedKey, new SerializableEntityCoordinates(
                        coords.EntityId,
                        metaData?.EntityName,
                        coords.X,
                        coords.Y,
                        xform?.MapUid,
                        mapMeta?.EntityName));
                    break;
                case IPlayerSession player:
                    var name = AddPlayer(id, player, entities, players);
                    parsed.Add(parsedKey, new SerializablePlayer(player.AttachedEntity, name, player.UserId));
                    break;
                default:
                    parsed.Add(parsedKey, value);
                    break;
            }
        }

        return (JsonSerializer.SerializeToDocument(parsed, _jsonOptions), players, entities);
    }

    private void AddEntity(int id, EntityStringRepresentation rep, List<AdminLogEntity> entities, List<AdminLogPlayer> players)
    {
        foreach (var ent in entities)
        {
            if (ent.Uid == rep.Uid.Id)
                return;
        }

        entities.Add(new AdminLogEntity { Uid = rep.Uid.Id, Name = rep.Name });

        if (rep.Session != null)
        {
            var user = rep.Session.UserId.UserId;
            DebugTools.Assert(players.All(p => p.PlayerUserId != user));
            players.Add(new AdminLogPlayer { LogId = id, PlayerUserId = user });
        }
    }

    private string? AddPlayer(int id, IPlayerSession player, List<AdminLogEntity> entities, List<AdminLogPlayer> players)
    {
        var name = _metaQuery.TryGetComponent(player.AttachedEntity, out var metadata)
            ? metadata.EntityName
            : null;

        var user = player.UserId.UserId;
        foreach (var p in players)
        {
            if (p.PlayerUserId != user)
                continue;

            // Already added player.
            return name;
        }

        players.Add(new AdminLogPlayer { LogId = id, PlayerUserId = user });

        if (player.AttachedEntity is not { } uid)
            return null;

        DebugTools.Assert(entities.All(e => e.Uid != uid.Id));
        entities.Add(new AdminLogEntity { Uid = uid.Id, Name = name });
        return name;
    }
}
