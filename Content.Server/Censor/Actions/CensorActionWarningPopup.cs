using System.Text;
using Content.Server.Popups;
using Content.Shared.Censor;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Censor.Actions;

[UsedImplicitly]
public sealed class CensorActionWarningPopup : ICensorAction
{
    private string? _previous = null;

    public bool SkipCensor(string fullText, Dictionary<string, int> matchedText)
    {
        if (fullText == _previous)
        {
            _previous = null;
            return true;
        }

        _previous = fullText;
        return false;
    }

    public bool RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> matchedText,
        string censorTargetName,
        EntityManager entMan)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendJoin(", ", matchedText.Keys);

        entMan.System<PopupSystem>()
            .PopupCursor($"Warning for \"{stringBuilder}\"? Caught in the {censorTargetName}.",
                session,
                PopupType.LargeCaution);

        return true;
    }
}
