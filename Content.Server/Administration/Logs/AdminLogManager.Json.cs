using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.Administration.Logs.Converters;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

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

    private (JsonDocument Json, HashSet<Guid> Players) ToJson(
        Dictionary<string, object?> properties)
    {
        var players = new HashSet<Guid>();
        var parsed = new Dictionary<string, object?>();

        foreach (var key in properties.Keys)
        {
            var value = properties[key];
            value = value switch
            {
                ICommonSession player => new SerializablePlayer(player),
                EntityCoordinates entityCoordinates => new SerializableEntityCoordinates(_entityManager, entityCoordinates),
                _ => value
            };

            var parsedKey = NamingPolicy.ConvertName(key);
            parsed.Add(parsedKey, value);

            var entityId = properties[key] switch
            {
                EntityUid id => id,
                EntityStringRepresentation rep => rep.Uid,
                ICommonSession {AttachedEntity: {Valid: true}} session => session.AttachedEntity,
                IComponent component => component.Owner,
                _ => null
            };

            if (_entityManager.TryGetComponent(entityId, out ActorComponent? actor))
            {
                players.Add(actor.PlayerSession.UserId.UserId);
            }
        }

        return (JsonSerializer.SerializeToDocument(parsed, _jsonOptions), players);
    }
}
