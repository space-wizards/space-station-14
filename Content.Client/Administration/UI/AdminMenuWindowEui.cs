using Content.Client.Administration.UI.AdminAnnounce;
using Content.Client.Eui;
using Content.Shared.Administration;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI
{
    public sealed class AdminAnnounceEui : BaseEui
    {
        private readonly AdminAnnounceWindow _window;

        public AdminAnnounceEui()
        {
            _window = new AdminAnnounceWindow();
            _window.AnnounceButton.OnPressed += _ => 
            {
                var announcement = AdminAnnounceHelpers.NormalizeText(Rope.Collapse(_window.Announcement.TextRope));
                if (string.IsNullOrWhiteSpace(announcement))
                    return;

                var announceType = (AdminAnnounceType) (_window.AnnounceMethod.SelectedMetadata ?? AdminAnnounceType.Station);

                SendMessage(new AdminAnnounceEuiMsg.DoAnnounce
                {
                    Announcement = announcement,
                    Announcer = AdminAnnounceHelpers.NormalizeText(_window.Announcer.Text),
                    AnnounceType = announceType,
                    CloseAfter = !_window.KeepWindowOpen.Pressed,
                    ColorHex = AdminAnnounceHelpers.GetValidatedColorHex(announceType, _window.GetCurrentHex()),
                    SoundPath = _window.SoundPath.Text,
                    Sender = AdminAnnounceHelpers.NormalizeText(_window.Sender.Text),
                });
            };
        }

        public override void Opened() => _window.OpenCentered();
        public override void Closed() => _window.Close();
    }
}
