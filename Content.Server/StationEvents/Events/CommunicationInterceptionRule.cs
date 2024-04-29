using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.StationEvents.Events;

public sealed class CommunicationInterceptionRule : StationEventSystem<CommunicationInterceptionRuleComponent>
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    protected override void Started(EntityUid uid, CommunicationInterceptionRuleComponent component, GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        if (_alertLevelSystem.GetLevel(chosenStation.Value) != "green")
            return;

        _alertLevelSystem.SetLevel(chosenStation.Value, component.AlertLevel, true, true, true);
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("station-event-communication-interception"), playSound: false, colorOverride: Color.Gold);
        _audioSystem.PlayGlobal("/Audio/Announcements/intercept.ogg", Filter.Broadcast(), true);
    }
}
