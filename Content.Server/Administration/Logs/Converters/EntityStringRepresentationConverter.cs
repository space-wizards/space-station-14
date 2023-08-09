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
    [Dependency] private readonly IPlayerManager _playerManager = default!;
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

            if (value.Session is IPlayerSession playerSession &&
                _adminManager.IsAdmin(playerSession))
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

        if (_entityManager.TryGetComponent<LubedComponent>(value.Uid, out var lubedComponent))
        {
            writer.WriteNumber("lubeSlipStrength", lubedComponent.SlipStrength);
            writer.WriteNumber("lubeSlipsLeft", lubedComponent.SlipsLeft);
        }

        if (_entityManager.TryGetComponent<GluedComponent>(value.Uid, out var gluedComponent))
        {
            writer.WriteNumber("gluedDuration", gluedComponent.Duration.TotalSeconds);
        }

        if (_entityManager.TryGetComponent<FlammableComponent>(value.Uid, out var flammableComponent) && flammableComponent.OnFire)
        {
            writer.WriteNumber("fireStacks", flammableComponent.FireStacks);
        }

        writer.WriteEndObject();
    }
}
