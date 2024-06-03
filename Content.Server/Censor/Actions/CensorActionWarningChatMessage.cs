using System.Globalization;
using Content.Server.Chat.Managers;
using Content.Shared.Censor;
using Robust.Shared.Player;

namespace Content.Server.Censor.Actions;

public sealed class CensorActionWarningChatMessage : ICensorAction
{
    public bool SkipCensor(string fullText, Dictionary<string, int> matchedText)
    {
        return false;
    }

    public bool RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> matchedText,
        string censorTargetName,
        IEntityManager entMan)
    {
        IoCManager.Resolve<IChatManager>()
            .DispatchServerMessage(session,
                Loc.GetString("censor-warning"));

        return true;
    }
}
