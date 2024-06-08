using Content.Server.Administration.Managers;
using Content.Shared.Automod;
using Content.Shared.Database;
using Robust.Shared.Player;

namespace Content.Server.Automod.Actions;

public sealed partial class TextAutomodActionBan : ITextAutomodAction
{
    [DataField]
    public uint? BanTime;

    [DataField]
    public string Reason = "automod-action-ban-reason";

    [DataField]
    public string BanCountGroup = "default";

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
        var banManager = IoCManager.Resolve<IBanManager>();

        // TODO ShadowCommander implement ban group tracking for banning after three filter hits within $time minutes

        var str = Loc.GetString(Reason, ("filterName", filterDisplayName));

        banManager.CreateServerBan(session.UserId, null, null, null, null, BanTime, NoteSeverity.Minor, str);

        return false;
    }
}
