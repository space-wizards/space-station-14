using System.Text.Json;
using Content.Server.Administration.Managers;
using Content.Server.Atmos.Components;
using Content.Shared.Glue;
using Content.Shared.Lube;
using Robust.Server.Player;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class EntityStringRepresentationConverter : AdminLogConverter<EntityStringRepresentation>
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

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

        if (_entityManager.TryGetComponent<FlammableComponent>(value.Uid, out var flammableComponent) && flammableComponent.OnFire)
        {
            writer.WriteStartObject("flammable");
            writer.WriteNumber("fireStacks", flammableComponent.FireStacks);
            writer.WriteEndObject();
        }

        if (_entityManager.TryGetComponent<GluedComponent>(value.Uid, out var gluedComponent))
        {
            writer.WriteStartObject("glued");
            writer.WriteNumber("duration", gluedComponent.Duration.TotalSeconds);
            writer.WriteEndObject();
        }

        if (_entityManager.TryGetComponent<LubedComponent>(value.Uid, out var lubedComponent))
        {
            writer.WriteStartObject("lubed");
            writer.WriteNumber("slipStrength", lubedComponent.SlipStrength);
            writer.WriteNumber("slipsLeft", lubedComponent.SlipsLeft);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
