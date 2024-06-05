using System.Text;
using Content.Server.Chat.Managers;
using Content.Shared.Automod;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Automod.Actions;

[UsedImplicitly]
public sealed class TextAutomodActionWarningChatMessage : ITextAutomodAction
{
    [DataField]
    public string Reason = "automod-action-warning-chat-reason";

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

        IoCManager.Resolve<IChatManager>().DispatchServerMessage(session, str);

        return true;
    }
}
