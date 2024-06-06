using Content.Server.Administration.Managers;
using Content.Shared.Automod;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Automod.Actions;

[UsedImplicitly]
public sealed partial class TextAutomodActionBan : ITextAutomodAction
{
    [DataField]
    public uint? BanTime;

    [DataField]
    public string Reason = "automod-action-ban-reason";

    [DataField]
    public string BanCountGroup = "default";

    public bool Skip(string fullText, Dictionary<string, int> patternMatches)
    {
        return false;
    }

    public bool RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> patternMatches,
        AutomodFilterDef filter,
        string filterDisplayName,
        IEntityManager entMan)
    {
        var banManager = IoCManager.Resolve<IBanManager>();

        // TODO ShadowCommander implement ban group tracking for banning after three filter hits within $time minutes

        var str = Loc.GetString(Reason, ("censorName", filterDisplayName));

        banManager.CreateServerBan(session.UserId, null, null, null, null, BanTime, NoteSeverity.Minor, str);

        return false;
    }
}
