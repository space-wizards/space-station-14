using Content.Shared.Automod;
using Robust.Shared.Player;

namespace Content.Server.Automod.Actions;

public sealed partial class TextAutomodActionBlockMessage : ITextAutomodAction
{
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
        return false;
    }
}
