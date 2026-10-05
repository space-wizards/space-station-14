using Content.Server.StationEvents.Components;
using Content.Shared.AlertLevel;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events setting the station alert level.
/// </summary>
/// <seealso cref="AlertLevelInterceptionRuleComponent"/>
public sealed partial class AlertLevelInterceptionRule : StationEventSystem<AlertLevelInterceptionRuleComponent>
{
    [Dependency] private AlertLevelSystem _alertLevel = default!;

    protected override void Started(Entity<AlertLevelInterceptionRuleComponent, GameRuleComponent> ent,
        ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(out var chosenStation))
            return;

        if (!_alertLevel.TryGetLevel(chosenStation.Value.Owner, out var level)
            || !_alertLevel.TryGetDefaultLevel(chosenStation.Value.Owner, out var defaultLevel)
            || level != defaultLevel)
            return;

        _alertLevel.SetLevel(chosenStation.Value.Owner,
            ent.Comp1.AlertLevel,
            force: true);
    }
}
