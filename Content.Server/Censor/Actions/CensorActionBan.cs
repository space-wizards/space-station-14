using Content.Server.Administration.Managers;
using Content.Shared.Censor;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Censor.Actions;

[UsedImplicitly]
public sealed class CensorActionBan : ICensorAction
{
    [DataField]
    public uint? BanTime;

    [DataField]
    public string Reason = "censor-action-ban-reason";

    [DataField]
    public string BanCountGroup = "default";

    public bool SkipCensor(string fullText, Dictionary<string, int> matchedText)
    {
        return false;
    }

    public bool RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> matchedText,
        TextCensorActionDef censor,
        IEntityManager entMan)
    {
        var banManager = IoCManager.Resolve<IBanManager>();

        // TODO ShadowCommander implement ban group tracking for banning after three censor hits within $time minutes

        banManager.CreateServerBan(session.UserId,
            null,
            null,
            null,
            null,
            BanTime,
            NoteSeverity.Minor,
            Loc.GetString(Reason, ("censorName", censor)));

        return false;
    }
}
