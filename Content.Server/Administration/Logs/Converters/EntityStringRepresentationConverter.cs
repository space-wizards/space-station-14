using System.Text.Json;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class EntityStringRepresentationConverter : AdminLogConverter<EntityStringRepresentation>
{
    public override void Write(Utf8JsonWriter writer, EntityStringRepresentation value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("id", (int) value.Uid);

        if (value.Name != null)
        {
            writer.WriteString("name", value.Name);
        }

        if (value.Session != null)
        {
            writer.WriteString("player", value.Session.UserId.UserId);
        }

        writer.WriteEndObject();
    }
}
