using System.Text.Json;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public class EntityJsonConverter : AdminLogConverter<Entity>
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("id", (int) value.Uid);
        writer.WriteString("name", value.Name);

        if (_entities.TryGetComponent(value.Uid, out ActorComponent? actor))
        {
            writer.WriteString("player", actor.PlayerSession.UserId.UserId);
        }

        writer.WriteEndObject();
    }
}
