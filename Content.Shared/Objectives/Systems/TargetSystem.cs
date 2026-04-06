using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Filters;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Random;

namespace Content.Shared.Objectives.Systems;

/// <summary>
/// This system stores enumerators to find valid Targets, typically searching for minds.
/// Typically used in conjunction with a <see cref="GameRuleSystem{T}"/> or an Objective.
/// </summary>
public sealed class TargetSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedStationAiSystem _ai = default!;

    private HashSet<Entity<MindComponent>> _pickingMinds = new();

    /// <summary>
    /// Returns a list of every living humanoid player's minds, except for a single one which is exluded.
    /// A new hashset is allocated for every call, consider using <see cref="AddAliveHumans"/> instead.
    /// </summary>
    public HashSet<Entity<MindComponent>> GetAliveHumans(EntityUid? exclude = null)
    {
        var allHumans = new HashSet<Entity<MindComponent>>();
        AddAliveHumans(allHumans, exclude);
        return allHumans;
    }

    /// <summary>
    /// Adds to a hashset every living humanoid player's minds, except for a single one which is exluded.
    /// </summary>
    public void AddAliveHumans(HashSet<Entity<MindComponent>> allHumans, EntityUid? exclude = null)
    {
        // HumanoidProfileComponent is used to prevent mice, pAIs, etc from being chosen
        var query = EntityQueryEnumerator<HumanoidProfileComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState))
        {
            // the player needs to have a mind and not be the excluded one +
            // the player has to be alive
            if (!_mind.TryGetMind(uid, out var mind, out var mindComp) || mind == exclude || !_mobState.IsAlive(uid, mobState))
                continue;

            allHumans.Add((mind, mindComp));
        }
    }

    /// <summary>
    /// Adds to a hashset every living AI core except for an optional single excluded mind.
    /// </summary>
    public void AddAliveAi(HashSet<Entity<MindComponent>> allAi, EntityUid? exclude = null)
    {
        // HumanoidProfileComponent is used to prevent mice, pAIs, etc from being chosen
        var query = EntityQueryEnumerator<StationAiCoreComponent, StationAiHolderComponent>();
        while (query.MoveNext(out var uid, out _, out var aiHolder))
        {
            // the player needs to have a mind and not be the excluded one +
            // the player has to be alive
            if (!_ai.TryGetHeld((uid, aiHolder), out var held) || _mobState.IsDead(held.Value))
                continue;

            if (!_mind.TryGetMind(held.Value, out var mind, out var mindComp) || mind == exclude)
                continue;

            allAi.Add((mind, mindComp));
        }
    }

    /// <summary>
    /// Picks a random mind from a pool after applying a list of filters.
    /// Returns null if no valid mind could be found.
    /// </summary>
    public Entity<MindComponent>? PickFromPool(MindPool pool, List<MindFilter> filters, EntityUid? exclude = null)
    {
        _pickingMinds.Clear();
        pool.FindMinds(_pickingMinds, exclude, EntityManager, this);
        _mind.FilterMinds(_pickingMinds, filters, exclude);

        if (_pickingMinds.Count == 0)
            return null;

        return _random.Pick(_pickingMinds);
    }
}
