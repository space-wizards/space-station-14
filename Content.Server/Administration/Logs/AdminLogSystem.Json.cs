using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.Administration.Logs.Converters;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Logs;

public partial class AdminLogSystem
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

    private (JsonDocument json, List<Guid> players, List<(int id, string? name)> entities) ToJson(
        Dictionary<string, object?> properties)
    {
        var entities = new List<(int id, string? name)>();
        var players = new List<Guid>();
        var parsed = new Dictionary<string, object?>();

        foreach (var key in properties.Keys)
        {
            var value = properties[key];
            value = value switch
            {
                IPlayerSession player => new SerializablePlayer(player),
                _ => value
            };

            var parsedKey = NamingPolicy.ConvertName(key);
            parsed.Add(parsedKey, value);

            EntityUid? entityId = properties[key] switch
            {
                EntityUid id => id,
                IEntity entity => entity,
                IPlayerSession {AttachedEntityUid: { }} session => session.AttachedEntityUid.Value,
                IComponent component => component.OwnerUid,
                _ => null
            };

            if (entityId is not { } uid)
            {
                continue;
            }

            var entityName = _entityManager.TryGetComponent(uid, out MetaDataComponent? metadata)
                ? metadata.EntityName
                : null;

            if (entities.Any(e => e.id == (int) uid)) continue;

            entities.Add(((int) uid, entityName));

            if (_entityManager.TryGetComponent(uid, out ActorComponent? actor))
            {
                players.Add(actor.PlayerSession.UserId.UserId);
            }
        }

        return (JsonSerializer.SerializeToDocument(parsed, _jsonOptions), players, entities);
    }
}
