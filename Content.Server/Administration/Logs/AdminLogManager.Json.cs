using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.Administration.Logs.Converters;
using Content.Server.Database;
using Robust.Server.GameObjects;
using Robust.Server.Player;

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
            (instance as IAdminLogConverter)?.Init(_dependencies);
            _jsonOptions.Converters.Add(instance);
        }

        var converterNames = _jsonOptions.Converters.Select(converter => converter.GetType().Name);
        _sawmill.Debug($"Admin log converters found: {string.Join(" ", converterNames)}");
    }

    private (JsonDocument Json, HashSet<Guid> Players, List<AdminLogEntity> Entities) ToJson(
        Dictionary<string, object?> properties)
    {
        var entities = new Dictionary<EntityUid, AdminLogEntity>();
        var players = new HashSet<Guid>();
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

            var entityId = properties[key] switch
            {
                EntityUid id => id,
                EntityStringRepresentation rep => rep.Uid,
                IPlayerSession {AttachedEntity: {Valid: true}} session => session.AttachedEntity,
                IComponent component => component.Owner,
                _ => null
            };

            if (entityId is not { } uid)
            {
                continue;
            }

            var entityName = _entityManager.TryGetComponent(uid, out MetaDataComponent? metadata)
                ? metadata.EntityName
                : null;

            // TODO set the id too whenever we feel like running a migration for 10 hours
            entities.TryAdd(uid, new AdminLogEntity { Name = entityName });

            if (_entityManager.TryGetComponent(uid, out ActorComponent? actor))
            {
                players.Add(actor.PlayerSession.UserId.UserId);
            }
        }

        return (JsonSerializer.SerializeToDocument(parsed, _jsonOptions), players, entities.Values.ToList());
    }
}
