using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist.Conditions;

public sealed partial class ConditionPlaytime : WhitelistCondition
{
    public int MinimumPlaytime = 0; // In minutes

    public override async Task<(bool, string)> Condition(NetUserData data)
    {
        var db = IoCManager.Resolve<IServerDbManager>();
        var playtime = await db.GetPlayTimes(data.UserId);
        var tracker = playtime.Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall);
        if (tracker is null)
        {
            return (false, Loc.GetString("whitelist-playtime", ("minutes", MinimumPlaytime)));
        }

        return (tracker.TimeSpent.TotalMinutes >= MinimumPlaytime, Loc.GetString("whitelist-playtime", ("minutes", MinimumPlaytime)));
    }

}
