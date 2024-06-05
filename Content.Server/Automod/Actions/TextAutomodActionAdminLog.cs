using System.Text;
using Content.Server.Administration.Logs;
using Content.Shared.Automod;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Automod.Actions;

[UsedImplicitly]
public sealed class TextAutomodActionAdminLog : ITextAutomodAction
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
        AutomodFilterDef automod,
        IEntityManager entMan)
    {
        IoCManager.Resolve<IAdminLogManager>()
            .Add(
                LogType.TextAutomod,
                Impact,
                $"{session.Name} ({session.UserId}) tripped {automod.DisplayName} ({automod.Id}) which matched \"{
                    new StringBuilder().AppendJoin(", ", patternMatches.Keys)}\"");

        return true;
    }
}
