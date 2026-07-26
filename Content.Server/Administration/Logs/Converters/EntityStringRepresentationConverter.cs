using System.Text.Json;

namespace Content.Server.Administration.Logs.Converters;

/// <summary>
/// Serializes <see cref="EntityStringRepresentation"/> to a structured JSON object.
/// </summary>
/// <remarks>
/// <para>Output shape, camelCase, nulls omitted:</para>
/// <code>
/// { "id": 1234, "name": "John Doe", "player": "GUID", "prototype": "MobHuman", "deleted": true }
/// </code>
/// <para>
/// The <c>player</c> field is only written when the entity has an attached session.
/// The <c>prototype</c> field is the prototype ID
/// The <c>deleted</c> field is only written when true.
/// </para>
/// </remarks>
[AdminLogConverter]
public sealed partial class EntityStringRepresentationConverter : AdminLogConverter<EntityStringRepresentation>
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
            // GUID
            writer.WriteString("player", value.Session.UserId.UserId);
        }

        if (value.Prototype != null)
        {
            writer.WriteString("prototype", value.Prototype);
        }

        if (value.Deleted)
        {
            writer.WriteBoolean("deleted", true);
        }

        writer.WriteEndObject();
    }
}
