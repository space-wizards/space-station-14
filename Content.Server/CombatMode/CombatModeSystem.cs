using Content.Server.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.Database;

namespace Content.Server.CombatMode;

public sealed class CombatModeSystem : SharedCombatModeSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void SetInCombatMode(EntityUid entity, bool value, CombatModeComponent? component = null)
    {
        if (!Resolve(entity, ref component, false))
            return;

        if (component.IsInCombatMode == value)
            return;

        base.SetInCombatMode(entity, value, component);

        _adminLogger.Add(
            LogType.CombatModeToggle,
            LogImpact.Low,
            $"{entity:actor} toggled combat mode {(value ? "on" : "off")}",
            new { state = value ? "on" : "off" });
    }
}
