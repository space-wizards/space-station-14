using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Shared.Mind;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class MindStringRepresentationConverter : AdminLogConverter<MindStringRepresentation>
{
    private JsonConverter<EntityStringRepresentation> _converter = null!;

    public override void Init2(JsonSerializerOptions options)
    {
        base.Init2(options);

        _converter = (JsonConverter<EntityStringRepresentation>)
            options.GetConverter(typeof(EntityStringRepresentation));
    }

    public override void Write(Utf8JsonWriter writer, MindStringRepresentation value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.OwnedEntity is { } owned)
        {
            writer.WritePropertyName("owned");
            _converter.Write(writer, owned, options);
        }

        if (value.Player is { } player)
        {
            writer.WriteString("player", player);
            writer.WriteBoolean("present", value.PlayerPresent);
        }

        writer.WriteEndObject();
    }
}
