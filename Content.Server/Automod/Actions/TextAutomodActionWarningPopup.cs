using System.Text;
using Content.Server.Popups;
using Content.Shared.Automod;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.Automod.Actions;

public sealed partial class TextAutomodActionWarningPopup : ITextAutomodAction
{
    [DataField]
    public string Reason = "automod-action-warning-popup-reason";

    public bool Skip(string fullText, List<(string match, int index)> patternMatches)
    {
        return false;
    }

    public bool RunAction(ICommonSession session,
        string fullText,
        List<(string match, int index)> patternMatches,
        AutomodFilterDef filter,
        string filterDisplayName,
        IEntityManager entMan)
    {
        var str = Loc.GetString(
            Reason,
            ("matches", string.Join(", ", patternMatches)),
            ("filterName", filterDisplayName));

        entMan.System<PopupSystem>().PopupCursor(str, session, PopupType.LargeCaution);

        return true;
    }
}
