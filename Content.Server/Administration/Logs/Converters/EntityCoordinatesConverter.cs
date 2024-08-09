using System.Text.Json;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class EntityCoordinatesConverter : AdminLogConverter<EntityCoordinates>
{
    // System.Text.Json actually keeps hold of your JsonSerializerOption instances in a cache on .NET 7.
    // Use a weak reference to avoid holding server instances live too long in integration tests.
    private WeakReference<IEntityManager> _entityManager = default!;

    public override void Init(IDependencyCollection dependencies)
    {
        _entityManager = new WeakReference<IEntityManager>(dependencies.Resolve<IEntityManager>());
    }

    public void Write(Utf8JsonWriter writer, EntityCoordinates value, JsonSerializerOptions options, IEntityManager entities)
    {
        writer.WriteStartObject();
        WriteEntityInfo(writer, value.EntityId, entities, "parent");
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        var mapUid = value.GetMapUid(entities);
        if (mapUid.HasValue)
        {
            WriteEntityInfo(writer, mapUid.Value, entities, "map");
        }
        writer.WriteEndObject();
    }

    private static void WriteEntityInfo(Utf8JsonWriter writer, EntityUid value, IEntityManager entities, string rootName)
    {
        writer.WriteStartObject(rootName);
        writer.WriteNumber("uid", value.Id);
        if (entities.TryGetComponent(value, out MetaDataComponent? metaData))
        {
            writer.WriteString("name", metaData.EntityName);
        }
        if (entities.TryGetComponent(value, out MapComponent? mapComponent))
        {
            writer.WriteNumber("mapId", mapComponent.MapId.GetHashCode());
            writer.WriteBoolean("mapPaused", mapComponent.MapPaused);
        }
        if (entities.TryGetComponent(value, out StationMemberComponent? stationMemberComponent))
        {
            WriteEntityInfo(writer, stationMemberComponent.Station, entities, "stationMember");
        }

        writer.WriteEndObject();
    }

    public override void Write(Utf8JsonWriter writer, EntityCoordinates value, JsonSerializerOptions options)
    {
        if (!_entityManager.TryGetTarget(out var entityManager))
            throw new InvalidOperationException("EntityManager got garbage collected!");

        Write(writer, value, options, entityManager);
    }
}
