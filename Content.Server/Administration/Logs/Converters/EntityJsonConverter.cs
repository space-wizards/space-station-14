using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Server.Administration.Logs.Converters;

public class EntityJsonConverter : JsonConverter<Entity>
{
    public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("id", (int) value.Uid);
        writer.WriteString("name", value.Name);
        writer.WriteString("prototype", value.Prototype?.Name);

        writer.WriteEndObject();
    }
}
