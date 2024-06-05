using System.Text;
using Content.Server.Chat.Managers;
using Content.Shared.Censor;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Censor.Actions;

[UsedImplicitly]
public sealed class CensorActionWarningChatMessage : ICensorAction
{
    [DataField]
    public string Reason = "censor-action-warning-chat-reason";

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
        IoCManager.Resolve<IChatManager>()
            .DispatchServerMessage(session,
                Loc.GetString(Reason,
                    ("matches", new StringBuilder().AppendJoin(", ", matchedText.Keys)),
                    ("censorName", censor.DisplayName)));

        return true;
    }
}
