using static Content.Client.Communications.UI.CommunicationsConsoleSheetlet;
using Content.Shared.Administration.AdminAnnounce;
using Content.Shared.CCVar;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.AdminAnnounce;

public sealed partial class AdminAnnounceWindow
{
    private void UpdateControls()
    {
        var isStation = GetSelectedAnnounceType() == AdminAnnounceType.Station;
        var message = Rope.Collapse(Announcement.TextRope);
        var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
        var tooLong = message.Length > maxLength;

        Signature.Editable = isStation && EnableSignature.Pressed;
        AnnounceButton.Disabled = string.IsNullOrWhiteSpace(message) || tooLong;
        AnnounceButton.ToolTip = tooLong
            ? Loc.GetString("comms-console-message-too-long")
            : null;

        UpdateCharacterLimitLabel(AnnounceCharLimitLabel, message.Length, maxLength);

        PlayAudio.Disabled = !isStation || GetSound() == null;
        StopAudio.Disabled = !isStation;
    }
}
