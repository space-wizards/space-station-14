using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;
using Content.Server.Afk;
using Robust.Server.DataMetrics;

namespace Content.Server.Administration.Managers;

// Handles metrics reporting for active admin count and such.

public sealed partial class AdminManager
{
    private Dictionary<int, (int active, int afk, int deadminned)>? _adminOnlineCounts;

    private const int SentinelRankId = -1;

    [Dependency] private readonly IMetricsManager _metrics = default!;
    [Dependency] private readonly IAfkManager _afkManager = default!;
    [Dependency] private readonly IMeterFactory _meterFactory = default!;

    private void InitializeMetrics()
    {
        _metrics.UpdateMetrics += MetricsOnUpdateMetrics;

        var meter = _meterFactory.Create("SS14.AdminManager");

        meter.CreateObservableGauge(
            "admins_online_count",
            MeasureAdminCount,
            null,
            "The count of online admins");
    }

    private void MetricsOnUpdateMetrics()
    {
        _sawmill.Verbose("Updating metrics");

        var dict = new Dictionary<int, (int active, int afk, int deadminned)>();

        foreach (var (session, reg) in _admins)
        {
            var rankId = reg.RankId ?? SentinelRankId;

            ref var counts = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, rankId, out _);

            if (reg.Data.Active)
            {
                if (_afkManager.IsAfk(session))
                    counts.afk += 1;
                else
                    counts.active += 1;
            }
            else
            {
                counts.deadminned += 1;
            }
        }

        // Neither prometheus-net nor dotnet-counters seem to handle stuff well if we STOP returning measurements.
        // i.e. if the last admin with a rank disconnects.
        // So if we have EVER reported a rank, always keep reporting it.
        if (_adminOnlineCounts != null)
        {
            foreach (var rank in _adminOnlineCounts.Keys)
            {
                CollectionsMarshal.GetValueRefOrAddDefault(dict, rank, out _);
            }
        }

        // Make sure "no rank" is always available. Avoid "no data".
        CollectionsMarshal.GetValueRefOrAddDefault(dict, SentinelRankId, out _);

        _adminOnlineCounts = dict;
    }

    private IEnumerable<Measurement<int>> MeasureAdminCount()
    {
        if (_adminOnlineCounts == null)
            yield break;

        foreach (var (rank, (active, afk, deadminned)) in _adminOnlineCounts)
        {
            yield return new Measurement<int>(
                active,
                new KeyValuePair<string, object?>("state", "active"),
                new KeyValuePair<string, object?>("rank", rank == SentinelRankId ? "none" : rank.ToString()));

            yield return new Measurement<int>(
                afk,
                new KeyValuePair<string, object?>("state", "afk"),
                new KeyValuePair<string, object?>("rank", rank == SentinelRankId ? "none" : rank.ToString()));

            yield return new Measurement<int>(
                deadminned,
                new KeyValuePair<string, object?>("state", "deadminned"),
                new KeyValuePair<string, object?>("rank", rank == SentinelRankId ? "none" : rank.ToString()));
        }
    }
}
