using Content.Client.Eui;
using Content.Shared.Administration;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI
{
    public sealed class AdminAnnounceEui : BaseEui
    {
        private readonly AdminAnnounceWindow _window;

        public AdminAnnounceEui()
        {
            _window = new AdminAnnounceWindow();
            _window.OnClose += () => SendMessage(new AdminAnnounceEuiMsg.Close());
            _window.AnnounceButton.OnPressed += AnnounceButtonOnOnPressed;
        }

        private void AnnounceButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            SendMessage(new AdminAnnounceEuiMsg.DoAnnounce
            {
                Announcement = _window.Announcement.Text,
                Announcer =  _window.Announcer.Text,
                AnnounceType =  (AdminAnnounceType) (_window.AnnounceMethod.SelectedMetadata ?? AdminAnnounceType.Station),
                CloseAfter = !_window.KeepWindowOpen.Pressed,
            });

        }

        public override void Opened()
        {
            _window.OpenCentered();
        }

        public override void Closed()
        {
            _window.Close();
        }
    }
}
