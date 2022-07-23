using System.Text.Json;
using System.Text.Json.Serialization;

namespace Content.Server.Administration.Logs.Converters;

public abstract class AdminLogConverter<T> : JsonConverter<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public abstract override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options);
}
