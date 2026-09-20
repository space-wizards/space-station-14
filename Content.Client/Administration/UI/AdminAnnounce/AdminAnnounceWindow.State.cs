using Content.Client.Communications.UI;
using Content.Shared.Administration.AdminAnnounce;
using Content.Shared.CCVar;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.AdminAnnounce;

public sealed partial class AdminAnnounceWindow
{
    private void UpdateButtons()
    {
        var message = Rope.Collapse(Announcement.TextRope);
        var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
        var tooLong = message.Length > maxLength;

        AnnounceButton.Disabled = string.IsNullOrWhiteSpace(message) || tooLong;
        AnnounceButton.ToolTip = tooLong
            ? Loc.GetString("comms-console-message-too-long")
            : null;

        AnnounceCharLimitLabel.SetOnlyStyleClass(tooLong
            ? CommunicationsConsoleSheetlet.CharLimitExceeded
            : CommunicationsConsoleSheetlet.CharLimit);
        AnnounceCharLimitLabel.Text = Loc.GetString(
            "comms-console-char-limit",
            ("count", message.Length),
            ("max", maxLength));

        var isStation = GetSelectedAnnounceType() == AdminAnnounceType.Station;
        PlayAudio.Disabled = !isStation || GetSound() == null;
        StopAudio.Disabled = !isStation;
    }
}
