using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared.Censor.Actions;

public sealed class CensorActionWarningPopup : ICensorAction
{
    private string? _previous = null;

    public bool AttemptCensor(string fullText, Dictionary<string, int> matchedText)
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
        Dictionary<string, int> matchedText,
        string censorTargetName,
        EntityManager entMan)
    {
        entMan.System<SharedPopupSystem>()
            .PopupCursor(
                $"Are you sure you want to send \"{matchedText}\"? This text has been caught in the {censorTargetName} filter.",
                session);
    }
}
