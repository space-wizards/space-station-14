using Content.Server.Shuttles.Systems;
using Content.Server.Station.Events;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.RoundEnd;

/// <summary>
///     Forces a shuttle call when necessary.
/// </summary>
public sealed partial class ShuttleCallerFailsafeSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configMan = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly EmergencyShuttleSystem _shuttleSys = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSys = default!;

    public static readonly string AnnouncementText = "round-end-system-shuttle-called-failsafe-announcement";
    private bool _shuttleEnabled;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);

        Subs.CVar(_configMan, CCVars.EmergencyShuttleEnabled, OnEmergencyShuttleEnabledChange, true);
        _shuttleEnabled = _configMan.GetCVar(CCVars.EmergencyShuttleEnabled);
    }

    private void OnEmergencyShuttleEnabledChange(bool value)
    {
        _shuttleEnabled = value;
    }

    private void OnStationPostInit(ref StationPostInitEvent args)
    {
        foreach (var uid in args.Station.Comp.Grids)
        {
            EnsureComp<ShuttleCallerFailsafeComponent>(uid);
        }
    }

    public bool ShouldCallShuttle()
    {
        var stationquery = EntityQueryEnumerator<ShuttleCallerFailsafeComponent>();
        while (stationquery.MoveNext(out var station, out var failsafecomp))
        {
            var callerQuery = EntityQueryEnumerator<ShuttleCallerComponent>();
            var callersFound = false;
            var stationMap = _transformSystem.GetMap(station);

            while (callerQuery.MoveNext(out var caller, out _))
            {
                if (_transformSystem.GetGrid(caller) == station ||
                    failsafecomp.IncludeCallersInSameMap &&
                    _transformSystem.GetMap(caller) == stationMap)
                {
                    callersFound = true;
                    break;
                }
            }

            if (!callersFound)
            {
                return true;
            }
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        if (!_shuttleEnabled)
        {
            return; // bruh
        }

        if (_shuttleSys.ShuttlesLeft || _shuttleSys.EmergencyShuttleArrived ||
            _roundEndSys.ExpectedCountdownEnd != null)
        {
            return; // The shuttle is either already called, here, or has left.
        }

        if (!ShouldCallShuttle())
        {
            return;
        }

        _roundEndSys.RequestRoundEnd(text: AnnouncementText);
    }
}
