using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.AlertLevel;
using Content.Shared.Communications;
using Content.Shared.Station;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Communications.UI
{
    public sealed class CommunicationsConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        private readonly SharedStationSystem _station = default!;
        private readonly AlertLevelSystem _alertLevel = default!;

        [ViewVariables]
        private CommunicationsConsoleMenu? _menu;

        public CommunicationsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _station = EntMan.System<SharedStationSystem>();
            _alertLevel = EntMan.System<AlertLevelSystem>();
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<CommunicationsConsoleMenu>();
            _menu.OnAnnounce += AnnounceButtonPressed;
            _menu.OnBroadcast += BroadcastButtonPressed;
            _menu.OnAlertLevel += AlertLevelSelected;
            _menu.OnEmergencyLevel += EmergencyShuttleButtonPressed;
        }

        public void AlertLevelSelected(ProtoId<AlertLevelPrototype> level)
        {
            if (_menu!.AlertLevelSelectable)
            {
                // TODO: This does not work until the console UI is predicted and uses component states.
                // Also someone decided to send BUI states regularly in an update loop, so this just gets randomly bulldozed until the message reaches the server.
                // _menu.CurrentAlertLevel = level;
                // _menu.AlertLevelSelectable = false;
                // _menu.AlertLevelButton.Disabled = true;
                SendMessage(new CommunicationsConsoleSelectAlertLevelMessage(level));
            }
        }

        public void EmergencyShuttleButtonPressed()
        {
            if (_menu!.CountdownStarted)
                RecallShuttle();
            else
                CallShuttle();
        }

        public void AnnounceButtonPressed(string message)
        {
            var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            var msg = SharedChatSystem.SanitizeAnnouncement(message, maxLength);
            SendMessage(new CommunicationsConsoleAnnounceMessage(msg));
        }

        public void BroadcastButtonPressed(string message)
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
                _menu.CanAnnounce = commsState.CanAnnounce;
                _menu.CanBroadcast = commsState.CanBroadcast;
                _menu.CanCall = commsState.CanCall;
                _menu.CountdownStarted = commsState.CountdownStarted;
                _menu.CountdownEnd = commsState.ExpectedCountdownEnd;

                _menu.CurrentAlertLevel = alertComp.CurrentAlertLevel;
                _menu.SelectableAlertLevels = _alertLevel.GetSelectableAlertLevels((stationUid.Value, alertComp));
                _menu.AlertLevelSelectable = _alertLevel.CanChangeAlertLevel((stationUid.Value, alertComp));

                _menu.UpdateCountdown();
                _menu.UpdateAlertLevels();

                _menu.AlertLevelButton.Disabled = !_menu.AlertLevelSelectable;
                _menu.EmergencyShuttleButton.Disabled = !_menu.CanCall;
                _menu.AnnounceButton.Disabled = !_menu.CanAnnounce;
                _menu.BroadcastButton.Disabled = !_menu.CanBroadcast;
            }
        }
    }
}
