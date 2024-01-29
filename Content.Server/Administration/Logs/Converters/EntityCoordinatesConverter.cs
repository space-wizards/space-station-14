using System.Text.Json;
using Content.Server.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class EntityCoordinatesConverter : AdminLogConverter<SerializableEntityCoordinates>
{
    // System.Text.Json actually keeps hold of your JsonSerializerOption instances in a cache on .NET 7.
    // Use a weak reference to avoid holding server instances live too long in integration tests.
    private WeakReference<IEntityManager> _entityManager = default!;

    public override void Init(IDependencyCollection dependencies)
    {
        _entityManager = new WeakReference<IEntityManager>(dependencies.Resolve<IEntityManager>());
    }

    public void Write(Utf8JsonWriter writer, SerializableEntityCoordinates value, JsonSerializerOptions options, IEntityManager entities)
    {
        writer.WriteStartObject();
        WriteEntityInfo(writer, value.EntityUid, entities, "parent");
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        if (value.MapUid.HasValue)
        {
            WriteEntityInfo(writer, value.MapUid.Value, entities, "map");
        }
        writer.WriteEndObject();
    }

    private static void WriteEntityInfo(Utf8JsonWriter writer, EntityUid value, IEntityManager entities, string rootName)
    {
        writer.WriteStartObject(rootName);
        writer.WriteNumber("uid", value.GetHashCode());
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

    public override void Write(Utf8JsonWriter writer, SerializableEntityCoordinates value, JsonSerializerOptions options)
    {
        if (!_entityManager.TryGetTarget(out var entityManager))
            throw new InvalidOperationException("EntityManager got garbage collected!");

        Write(writer, value, options, entityManager);
    }
}

public readonly struct SerializableEntityCoordinates
{
    public readonly EntityUid EntityUid;
    public readonly float X;
    public readonly float Y;
    public readonly EntityUid? MapUid;

    public SerializableEntityCoordinates(IEntityManager entityManager, EntityCoordinates coordinates)
    {
        EntityUid = coordinates.EntityId;
        X = coordinates.X;
        Y = coordinates.Y;
        MapUid = coordinates.GetMapUid(entityManager);
    }
}
