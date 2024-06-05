using Content.Server.Administration.Managers;
using Content.Shared.Automod;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Automod.Actions;

[UsedImplicitly]
public sealed class TextAutomodActionBan : ITextAutomodAction
{
    [DataField]
    public uint? BanTime;

    [DataField]
    public string Reason = "censor-action-ban-reason";

    [DataField]
    public string BanCountGroup = "default";

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
        var banManager = IoCManager.Resolve<IBanManager>();

        // TODO ShadowCommander implement ban group tracking for banning after three censor hits within $time minutes

        banManager.CreateServerBan(session.UserId,
            null,
            null,
            null,
            null,
            BanTime,
            NoteSeverity.Minor,
            Loc.GetString(Reason, ("censorName", automod)));

        return false;
    }
}
