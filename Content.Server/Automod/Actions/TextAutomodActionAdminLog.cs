using Content.Server.Administration.Logs;
using Content.Shared.Automod;
using Content.Shared.Database;
using Robust.Shared.Player;

namespace Content.Server.Automod.Actions;

public sealed partial class TextAutomodActionAdminLog : ITextAutomodAction
{
    [DataField]
    public LogImpact Impact = LogImpact.Medium;

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
        IoCManager.Resolve<IAdminLogManager>()
            .Add(
                LogType.TextAutomod,
                Impact,
                $"{entMan.ToPrettyString(session.AttachedEntity):player} ({session}) tripped {filterDisplayName
                } which matched: {string.Join("; ", patternMatches)}");

        return true;
    }
}
