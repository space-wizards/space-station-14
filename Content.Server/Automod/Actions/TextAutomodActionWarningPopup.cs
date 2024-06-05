using System.Text;
using Content.Server.Popups;
using Content.Shared.Automod;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Automod.Actions;

[UsedImplicitly]
public sealed class TextAutomodActionWarningPopup : ITextAutomodAction
{
    [DataField]
    public string Reason = "censor-action-warning-popup-reason";

    public bool Skip(string fullText, Dictionary<string, int> patternMatches)
    {
        return false;
    }

    public bool RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> patternMatches,
        AutomodFilterDef automod,
        IEntityManager entMan)
    {
        var str = Loc.GetString(
            Reason,
            ("matches", new StringBuilder().AppendJoin(", ", patternMatches.Keys)),
            ("censorName", automod.DisplayName),
            ("censorId", automod.Id is null ? "" : automod.Id));

        entMan.System<PopupSystem>().PopupCursor(str, session, PopupType.LargeCaution);

        return true;
    }
}
