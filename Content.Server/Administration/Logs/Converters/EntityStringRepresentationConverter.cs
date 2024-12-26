using System.Text.Json;
using Content.Server.Administration.Managers;
using Robust.Server.Player;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class EntityStringRepresentationConverter : AdminLogConverter<EntityStringRepresentation>
{
    [Dependency] private readonly IAdminManager _adminManager = default!;

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

            if (_adminManager.IsAdmin(value.Uid))
            {
                writer.WriteBoolean("admin", true);
            }
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
