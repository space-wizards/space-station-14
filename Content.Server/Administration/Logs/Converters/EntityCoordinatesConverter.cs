using System.Text.Json;
using Content.Server.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class EntityCoordinatesConverter : AdminLogConverter<SerializableEntityCoordinates>
{
    public override void Write(Utf8JsonWriter writer, SerializableEntityCoordinates value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteStartObject("parent");
        writer.WriteNumber("uid", value.Parent.Id);
        writer.WriteString("name", value.ParentName);
        writer.WriteEndObject();

        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);

        if (value.Station != null)
        {
            writer.WriteStartObject("station");
            writer.WriteNumber("uid", value.Station.Value.Id);
            writer.WriteString("name", value.StationName);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}

public readonly record struct SerializableEntityCoordinates(
    EntityUid Parent,
    string? ParentName,
    float X,
    float Y,
    EntityUid? Station,
    string? StationName);
