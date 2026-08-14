using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.AlertLevel;
using Content.Shared.Communications;
using Content.Shared.Station;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Communications.UI;

public sealed partial class CommunicationsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private AlertLevelSystem _alertLevel = default!;

    [ViewVariables]
    private CommunicationsConsoleMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CommunicationsConsoleMenu>();
        _menu.OnRadioAnnounce += RadioAnnounceButtonPressed;
        _menu.OnScreenBroadcast += ScreenBroadcastButtonPressed;
        _menu.OnAlertLevelChanged += AlertLevelSelected;
        _menu.OnShuttleCalled += CallShuttle;
        _menu.OnShuttleRecalled += RecallShuttle;

        if (EntMan.TryGetComponent<CommunicationsConsoleComponent>(Owner, out var console))
        {
            _menu.SetBroadcastDisplayEntity(console.ScreenDisplayId);
        }
    }

    public void AlertLevelSelected(ProtoId<AlertLevelPrototype> level)
    {
        // TODO: This does not work until the console UI is predicted and uses component states.
        // Also someone decided to send BUI states regularly in an update loop, so this just gets randomly bulldozed until the message reaches the server.
        // _menu.CurrentAlertLevel = level;
        // _menu.AlertLevelSelectable = false;
        // _menu.AlertLevelButton.Disabled = true;
        SendMessage(new CommunicationsConsoleSelectAlertLevelMessage(level));
    }

    public void RadioAnnounceButtonPressed(string message)
    {
        var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
        var msg = SharedChatSystem.SanitizeAnnouncement(message, maxLength);
        SendMessage(new CommunicationsConsoleAnnounceMessage(msg));
    }

    public void ScreenBroadcastButtonPressed(string message)
    {
        SendMessage(new CommunicationsConsoleBroadcastMessage(message));
    }

    public void CallShuttle()
    {
        SendMessage(new CommunicationsConsoleCallEmergencyShuttleMessage());
    }

    public void RecallShuttle()
    {
        SendMessage(new CommunicationsConsoleRecallEmergencyShuttleMessage());
    }

    // TODO: Use component states and update in an AfterAutoHandleState subscription
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CommunicationsConsoleInterfaceState commsState)
            return;

        var stationUid = _station.GetOwningStation(Owner);

        if (!EntMan.TryGetComponent<AlertLevelComponent>(stationUid, out var alertComp))
            return;

        if (_menu != null)
        {
            var currentAlertLevel = alertComp.CurrentAlertLevel;
            var selectableAlertLevels = _alertLevel.GetSelectableAlertLevels((stationUid.Value, alertComp));
            var canChangeAlertLevel = _alertLevel.CanChangeAlertLevel((stationUid.Value, alertComp));

            _menu.UpdateState(commsState, currentAlertLevel, selectableAlertLevels, canChangeAlertLevel);
        }
    }
}
