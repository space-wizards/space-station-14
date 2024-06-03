using System.Text.RegularExpressions;
using Content.Server.Administration.Managers;
using Content.Shared.Censor;
using Robust.Shared.Player;

namespace Content.Server.Censor.Actions;

public sealed class CensorActionBan : ICensorAction
{
    public bool AttemptCensor(string fullText, Dictionary<string, int> matchedText)
    {
        return true;
    }

    public void RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> matchedText,
        string censorTargetName,
        EntityManager entMan)
    {
        var banManager = IoCManager.Resolve<IBanManager>();


        // banManager.CreateServerBan(session.UserId, session.AttachedEntity.Value, player?.UserId, null, targetHWid,
        //     minutes, severity,
        //     reason);
    }
}
