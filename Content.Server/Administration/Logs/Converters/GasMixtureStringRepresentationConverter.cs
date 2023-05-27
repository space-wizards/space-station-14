using System.Text.Json;
using Content.Server.Atmos;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class GasMixtureStringRepresentationConverter : AdminLogConverter<GasMixtureStringRepresentation>
{
    public override void Write(Utf8JsonWriter writer, GasMixtureStringRepresentation value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("mol", value.TotalMoles);
        writer.WriteNumber("temperature", value.Temperature);
        writer.WriteNumber("pressure", value.Pressure);

        writer.WriteStartObject("gases");
        foreach (var x in value.MolesPerGas)
        {
            writer.WriteNumber(x.Key, x.Value);
        }
        writer.WriteEndObject();

        writer.WriteEndObject();
    }
}
