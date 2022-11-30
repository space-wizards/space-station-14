using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Corvax.JoinPlayTime;

public sealed class JoinPlayTimeManager
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    
    private int _minHours = 0;
    
    public void Initialize()
    {
        _cfg.OnValueChanged(CCVars.JoinPlaytimeHours, (v) => _minHours = v, true);

        _net.Connecting += OnConnecting;
    }

    private async Task OnConnecting(NetConnectingArgs arg)
    {
        var playTimes = await _db.GetPlayTimes(arg.UserId);
        var overallTime = playTimes.Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall);
        if (overallTime != null && overallTime.TimeSpent.TotalHours < _minHours)
        {
            arg.Deny(Loc.GetString("join-playtime-deny-reason", ("hours", _minHours)));
        }
    }
}
