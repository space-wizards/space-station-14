using System.Text;
using Content.Server.Administration.Logs;
using Content.Shared.Automod;
using Content.Shared.Database;
using Robust.Shared.Player;

namespace Content.Server.Automod.Actions;

public sealed partial class TextAutomodActionAdminLog : ITextAutomodAction
{
    [DataField]
    public LogImpact Impact = LogImpact.Medium;

    public bool Skip(string fullText, Dictionary<string, int> patternMatches)
    {
        return false;
    }

    public bool RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> patternMatches,
        AutomodFilterDef filter,
        string filterDisplayName,
        IEntityManager entMan)
    {
        IoCManager.Resolve<IAdminLogManager>()
            .Add(
                LogType.TextAutomod,
                Impact,
                $"{entMan.ToPrettyString(session.AttachedEntity):player} ({session}) tripped {filterDisplayName
                } which matched: {new StringBuilder().AppendJoin("; ", patternMatches.Keys)}");

        return true;
    }
}
