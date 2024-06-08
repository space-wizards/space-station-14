using System.Text;
using Content.Server.Chat.Managers;
using Content.Shared.Automod;
using Robust.Shared.Player;

namespace Content.Server.Automod.Actions;

public sealed partial class TextAutomodActionWarningChatMessage : ITextAutomodAction
{
    [DataField]
    public string Reason = "automod-action-warning-chat-reason";

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
        var str = Loc.GetString(
            Reason,
            ("matches", string.Join(", ", patternMatches)),
            ("filterName", filterDisplayName));

        IoCManager.Resolve<IChatManager>().DispatchServerMessage(session, str);

        return true;
    }
}
