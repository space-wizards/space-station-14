using System.Text.Json;
using System.Text.Json.Serialization;

namespace Content.Server.Administration.Logs.Converters;

public interface IAdminLogConverter
{
    void Init(IDependencyCollection dependencies);
}

public abstract class AdminLogConverter<T> : JsonConverter<T>, IAdminLogConverter
{
    public virtual void Init(IDependencyCollection dependencies)
    {
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public abstract override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options);
}
