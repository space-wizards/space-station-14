using System.Text.RegularExpressions;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared.Censor.Actions;

public sealed class CensorActionWarningPopup : ICensorAction
{
    private string? _previous = null;

    public bool IsCensored(string fullText, MatchCollection matchedText)
    {
        if (fullText == _previous)
        {
            _previous = null;
            return false;
        }

        _previous = fullText;
        return true;
    }

    public void RunAction(ICommonSession session,
        string fullText,
        MatchCollection matchedText,
        string censorTargetName,
        EntityManager entMan)
    {
        entMan.System<SharedPopupSystem>()
            .PopupCursor(
                $"Are you sure you want to send \"{matchedText}\"? This text has been caught in the {censorTargetName} filter.",
                session);
    }
}
