using static Content.Client.Stylesheets.Sheetlets.CharacterLimitSheetlet;
using Content.Shared.Administration.AdminAnnounce;
using Content.Shared.CCVar;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.AdminAnnounce;

public sealed partial class AdminAnnounceWindow
{
    private void UpdateControls()
    {
        var isStation = SelectedAnnounceType == AdminAnnounceType.Station;
        var message = Rope.Collapse(Announcement.TextRope);
        var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
        var tooLong = message.Length > maxLength;

        Signature.Editable = isStation;
        AnnounceButton.Disabled = string.IsNullOrWhiteSpace(message) || tooLong;
        AnnounceButton.ToolTip = tooLong
            ? Loc.GetString("comms-console-message-too-long")
            : null;

        UpdateLabel(AnnounceCharLimitLabel, message.Length, maxLength);

        PlayAudio.Disabled = !isStation || GetSound() == null;
        StopAudio.Disabled = !isStation;
    }
}
