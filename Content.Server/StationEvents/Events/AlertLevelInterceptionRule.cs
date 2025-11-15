using Content.Server.StationEvents.Components;
using Content.Shared.AlertLevel;
using Content.Shared.GameTicking.Components;

namespace Content.Server.StationEvents.Events;

public sealed class AlertLevelInterceptionRule : StationEventSystem<AlertLevelInterceptionRuleComponent>
{
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;

    protected override void Started(EntityUid uid, AlertLevelInterceptionRuleComponent component, GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        if (!_alertLevel.TryGetLevel(chosenStation.Value, out var level)
            || !_alertLevel.TryGetDefaultLevel(chosenStation.Value, out var defaultLevel)
            || level != defaultLevel)
            return;

        _alertLevel.SetLevel(chosenStation.Value,
            component.AlertLevel,
            force: true);
    }
}
