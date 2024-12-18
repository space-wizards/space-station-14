using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Communications;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client.Communications.UI
{
    public sealed class CommunicationsConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        [ViewVariables]
        private CommunicationsConsoleMenu? _menu;

        public CommunicationsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

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

        public void AlertLevelSelected(string level)
        {
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

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not CommunicationsConsoleInterfaceState commsState)
                return;

            if (_menu != null)
            {
                _menu.UpdateState(commsState);
            }
        }
    }
}
