using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
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
                var announcement = Rope.Collapse(_window.Announcement.TextRope).Trim();
                if (string.IsNullOrWhiteSpace(announcement))
                    return;

                SendMessage(new AdminAnnounceEuiMsg.DoAnnounce
                {
                    Announcement = announcement,
                    Announcer = _window.Announcer.Text.Trim(),
                    AnnounceType = (AdminAnnounceType) (_window.AnnounceMethod.SelectedMetadata ?? AdminAnnounceType.Station),
                    CloseAfter = !_window.KeepWindowOpen.Pressed,
                    ColorHex = AdminAnnounceHelpers.CleanHex(_window.ColorHex.Text),
                    SoundPath = _window.SoundPath.Text.Trim(),
                    Sender = _window.Sender.Text.Trim(),
                });
            };
        }

        public override void Opened() => _window.OpenCentered();
        public override void Closed() => _window.Close();
    }
}
