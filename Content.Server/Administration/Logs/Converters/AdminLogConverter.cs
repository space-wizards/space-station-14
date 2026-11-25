using System.Text.Json;
using System.Text.Json.Serialization;

namespace Content.Server.Administration.Logs.Converters;

public interface IAdminLogConverter
{
    void Init(IDependencyCollection dependencies);

    /// <summary>
    /// Called after all converters have been added to the <see cref="JsonSerializerOptions"/>.
    /// </summary>
    void Init2(JsonSerializerOptions options)
    {
    }
}

public abstract class AdminLogConverter<T> : JsonConverter<T>, IAdminLogConverter
{
    public virtual void Init(IDependencyCollection dependencies)
    {
    }

    public virtual void Init2(JsonSerializerOptions options)
    {
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public abstract override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options);
}
