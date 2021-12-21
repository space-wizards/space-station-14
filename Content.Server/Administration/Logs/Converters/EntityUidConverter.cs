using System.Text.Json;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public class EntityUidConverter : AdminLogConverter<EntityUid>
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public static void Write(Utf8JsonWriter writer, EntityUid value, JsonSerializerOptions options, IEntityManager entities)
    {
        writer.WriteStartObject();

        writer.WriteNumber("id", (int) value);

        if (entities.TryGetComponent(value, out MetaDataComponent metaData))
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
