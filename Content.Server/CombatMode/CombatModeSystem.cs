using System.Text.Json;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.Database;
using Robust.Shared.Player;

namespace Content.Server.CombatMode;

public sealed class CombatModeSystem : SharedCombatModeSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void SetInCombatMode(EntityUid entity, bool value, CombatModeComponent? component = null)
    {
        if (!Resolve(entity, ref component, false))
            return;

        if (component.IsInCombatMode == value)
            return;

        base.SetInCombatMode(entity, value, component);

        Guid[]? players = null;
        if (_player.TryGetSessionByEntity(entity, out var session))
            players = [session.UserId.UserId];

        _adminLogger.AddStructured(
            LogType.CombatModeToggle,
            LogImpact.Low,
            $"{entity:actor} toggled combat mode {(value ? "on" : "off")}",
            JsonSerializer.SerializeToDocument(new
            {
                entity = (int) entity,
                state = value ? "on" : "off"
            }),
            players: players,
            entities: [new AdminLogEntityRef(entity, AdminLogEntityRole.Actor)],
            playerRoles: players != null && session != null
                ? new Dictionary<Guid, AdminLogEntityRole> { [session.UserId.UserId] = AdminLogEntityRole.Actor }
                : null);
    }
}
