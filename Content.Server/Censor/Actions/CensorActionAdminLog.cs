using System.Text;
using Content.Server.Administration.Logs;
using Content.Shared.Censor;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Censor.Actions;

[UsedImplicitly]
public sealed class CensorActionAdminLog : ICensorAction
{
    [DataField]
    public LogImpact Impact = LogImpact.Medium;

    public bool SkipCensor(string fullText, Dictionary<string, int> matchedText)
    {
        return false;
    }

    public bool RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> matchedText,
        CensorFilterDef censor,
        IEntityManager entMan)
    {
        IoCManager.Resolve<IAdminLogManager>()
            .Add(LogType.Censor,
                Impact,
                $"{session.Name} ({session.UserId}) tripped {censor.DisplayName} which matched \"{new StringBuilder().AppendJoin(", ", matchedText.Keys)}\"");

        return true;
    }
}
