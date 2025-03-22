using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.RoundStatistics.Components;
using Content.Shared.GameTicking;
using Content.Shared.RoundStatistics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.RoundStatistics.Systems;

/// <summary>
/// Manages and displays round-end statistics, counting events and formatting results for the round summary.
/// </summary>
public sealed class RoundEndStatisticsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    private Entity<RoundEndStatisticsComponent>? _cachedEntity;

    private Dictionary<ProtoId<RoundStatisticPrototype>, int> Statistics => (_cachedEntity ??= FindOrCreateHolderEntity()).Comp.Statistics;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndStatisticsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RoundEndStatisticsComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<RoundEndStatisticsComponent, MapInitEvent>(OnComponentMapInit);
        SubscribeLocalEvent<RoundEndStatisticsComponent, PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<ChangeStatsValueEvent>(ChangeValue);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<RoundStatisticsAppendEvent>(OnRoundEndText);
    }

    private Entity<RoundEndStatisticsComponent> FindOrCreateHolderEntity()
    {
        // Try to find existing entity
        var query = EntityQueryEnumerator<RoundEndStatisticsComponent>();
        while (query.MoveNext(out var uid, out var statsComp))
        {
            return (uid, statsComp);
        }

        // Didn't find one, so create a new one
        return CreateHolderEntity();
    }

    private Entity<RoundEndStatisticsComponent> CreateHolderEntity()
    {
        // Create an empty entity in nullspace
        var uid = Spawn(null, MapCoordinates.Nullspace);
        _meta.SetEntityName(uid, "Round Statistics Holder");

        // Add holder component
        var statsComp = AddComp<RoundEndStatisticsComponent>(uid);
        return (uid, statsComp);
    }

    private void UpdatePrototypes(IEnumerable<RoundStatisticPrototype> protos)
    {
        foreach (var proto in protos)
        {
            // Initialize any new stats to 0, preserving existing stats.
            Statistics.TryAdd(proto.ID, 0);
        }
    }

    // Change the value by the given int
    private void ChangeValue(ref ChangeStatsValueEvent args)
    {
        var key = new ProtoId<RoundStatisticPrototype>(args.Key);

        if (Statistics.TryGetValue(key, out _))
        {
            Statistics[key] += args.Amount;
        }
        else
        {
            DebugTools.Assert(false);
        }
    }

    private void OnComponentInit(Entity<RoundEndStatisticsComponent> entity, ref ComponentInit args)
    {
        DebugTools.Assert(_cachedEntity == null);

        _cachedEntity = entity;
    }

    private void OnComponentShutdown(Entity<RoundEndStatisticsComponent> entity, ref ComponentShutdown args)
    {
        _cachedEntity = null;
    }

    private void OnComponentMapInit(Entity<RoundEndStatisticsComponent> entity, ref MapInitEvent args)
    {
        // Set up a counter for each statistic prototype
        var protos = _prototypeManager.EnumeratePrototypes<RoundStatisticPrototype>();
        UpdatePrototypes(protos);
    }

    private void OnPrototypesReloaded(Entity<RoundEndStatisticsComponent> entity, ref PrototypesReloadedEventArgs args)
    {
        if (args.TryGetModified<RoundStatisticPrototype>(out var modified))
        {
            // Add counters for any new statistics
            UpdatePrototypes(modified.Select(_prototypeManager.Index<RoundStatisticPrototype>));
        }
    }

    // Set all count to zero on roundstart
    private void OnRoundStart(RoundStartingEvent args)
    {
        foreach (var key in Statistics.Keys)
        {
            Statistics[key] = 0;
        }
    }

    // Format and send all statistics on roundend
    private void OnRoundEndText(RoundStatisticsAppendEvent args)
    {
        foreach (var (statId, count) in Statistics.Where(s => s.Value > 0))
        {
            if (!_prototypeManager.TryIndex(statId, out var stat))
            {
                Log.Warning($"Unknown RoundStatisticPrototype id '{statId}'");
                continue;
            }
            var text = Loc.GetString(stat.StatString, ("count", count));
            args.AddLine(text);
        }
    }
}
