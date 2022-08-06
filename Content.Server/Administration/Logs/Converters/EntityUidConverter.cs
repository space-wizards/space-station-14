using System.Text.Json;
using Robust.Server.GameObjects;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class EntityUidConverter : AdminLogConverter<EntityUid>
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public static void Write(Utf8JsonWriter writer, EntityUid value, JsonSerializerOptions options, IEntityManager entities)
    {
        writer.WriteStartObject();

        writer.WriteNumber("id", (int) value);

        if (entities.TryGetComponent(value, out MetaDataComponent? metaData))
        {
            writer.WriteString("name", metaData.EntityName);
        }

        if (entities.TryGetComponent(value, out ActorComponent? actor))
        {
            writer.WriteString("player", actor.PlayerSession.UserId.UserId);
        }

        writer.WriteEndObject();
    }

    public override void Write(Utf8JsonWriter writer, EntityUid value, JsonSerializerOptions options)
    {
        Write(writer, value, options, _entities);
    }
}
