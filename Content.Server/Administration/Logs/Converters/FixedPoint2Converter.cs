using System.Text.Json;
using Content.Shared.FixedPoint;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class FixedPoint2Converter : AdminLogConverter<FixedPoint2>
{
    public override void Write(Utf8JsonWriter writer, FixedPoint2 value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Int());
    }
}
