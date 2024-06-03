using System.Text.RegularExpressions;
using Content.Server.Administration.Managers;
using Content.Shared.Censor;
using Robust.Shared.Player;

namespace Content.Server.Censor.Actions;

public sealed class CensorActionBan : ICensorAction
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
        var banManager = IoCManager.Resolve<IBanManager>();


        // banManager.CreateServerBan(session.UserId, session.AttachedEntity.Value, player?.UserId, null, targetHWid,
        //     minutes, severity,
        //     reason);

        return false;
    }
}
