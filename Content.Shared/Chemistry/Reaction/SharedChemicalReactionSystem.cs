using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.Reaction;
public abstract partial class SharedChemicalReactionSystem : EntitySystem
{

    /// <summary>
    ///     The maximum number of reactions that may occur when a solution is changed.
    /// </summary>
    private const int MaxReactionIterations = 20;

    #region Dependency
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IGamePrototypeLoadManager _gamePrototypeLoadManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] protected readonly ISharedAdminLogManager _adminLogger = default!;
    #endregion Dependency

    /// <summary>
    /// A cache of all existant chemical reactions indexed by one of their required reactants.
    /// </summary>
    private Dictionary<string, List<ReactionSpecification>> _reactions = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AllReactionsStoppedMessage>(_onAllReactionsStopped);

        InitializeReactions();
        _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;
        _gamePrototypeLoadManager.GamePrototypeLoaded += InitializeReactions;
    }

    /// <summary>
    /// Handles building the reaction cache.
    /// </summary>
    private void InitializeReactions()
    {
        var reactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>();
        foreach(var reaction in reactions)
        {
            InitializeReaction(reaction);
        }
    }

    /// <summary>
    /// Caches a reaction by its first required reagent.
    /// Used to build the reaction cache.
    /// </summary>
    /// <param name="reaction">A reaction prototype to cache.</param>
    protected void InitializeReaction(ReactionSpecification reaction)
    {
        var removedHeatCapacity = 0f;
        var totalVolumeDelta = FixedPoint2.Zero;
        foreach(var reactant in reaction.Reactants)
        {
            if (reactant.Catalyst)
                continue;

            var consumedReagent = _prototypeManager.Index<ReagentPrototype>(reactant.Id);
            removedHeatCapacity += consumedReagent.SpecificHeat * (float)reactant.Amount;
            totalVolumeDelta -= reactant.Amount;
        }

        var addedHeatCapacity = 0f;
        foreach(var product in reaction.Products)
        {
            var producedReagent = _prototypeManager.Index<ReagentPrototype>(product.Id);
            addedHeatCapacity += producedReagent.SpecificHeat * (float)product.Amount;
            totalVolumeDelta += product.Amount;
        }
        reaction.HeatCapacityDelta = addedHeatCapacity - removedHeatCapacity;
        reaction.ProductTemperature = (addedHeatCapacity != 0f) ? reaction.HeatDelta / addedHeatCapacity : 0;
        reaction.VolumeDelta = totalVolumeDelta;

        if (reaction.Reactants.Count <= 0)
            return;

        List<ReactionSpecification>? cache;
        var cacheReagent = reaction.Reactants[0].Id;
        if(!_reactions.TryGetValue(cacheReagent, out cache))
        {
            cache = new();
            _reactions.Add(cacheReagent, cache);
        }
        cache.Add(reaction);
    }

    /// <summary>
    ///     Updates the reaction cache when the prototypes are reloaded.
    /// </summary>
    /// <param name="eventArgs">The set of modified prototypes.</param>
    protected void OnPrototypesReloaded(PrototypesReloadedEventArgs eventArgs)
    {
        if (!eventArgs.ByType.TryGetValue(typeof(ReactionPrototype), out var set))
            return;

        foreach (var (reactant, cache) in _reactions)
        {
            cache.RemoveAll((reaction) => set.Modified.ContainsKey(reaction.ID));
            if (cache.Count <= 0)
                _reactions.Remove(reactant);
        }

        foreach (var prototype in set.Modified.Values)
        {
            InitializeReaction((ReactionSpecification) prototype);
        }

        var curTime = _timing.CurTime;
        foreach(var reacting in EntityQuery<ReactingComponent>())
        {
            var uid = reacting.Owner;
            var deadGroups = new List<Solution>();
            foreach(var (solution, reactionGroup) in reacting.ReactionGroups)
            {
                List<ReactionSpecification> toRemove = new();
                foreach(var (reaction, reactionData) in reactionGroup)
                {
                    if (set.Modified.ContainsKey(reaction.ID))
                    {
                        OnReactionStop(reaction, uid, solution, curTime, reactionData);
                    }
                }

                foreach(var reaction in toRemove)
                {
                    reactionGroup.Remove(reaction);
                }

                if (reactionGroup.Count <= 0)
                    deadGroups.Add(solution);
            }

            foreach(var group in deadGroups)
            {
                reacting.ReactionGroups.Remove(group);
            }

            if (reacting.ReactionGroups.Count <= 0)
                QueueLocalEvent(new AllReactionsStoppedMessage(uid, reacting));
        }
    }
}

/// <summary>
///     Raised directed at the owner of a solution to determine whether the reaction should be allowed to occur.
/// </summary>
/// <reamrks>
///     Some solution containers (e.g., bloodstream, smoke, foam) use this to block certain reactions from occurring.
/// </reamrks>
[ByRefEvent]
public class ReactionAttemptEvent : CancellableEntityEventArgs
{
    public readonly ReactionSpecification Reaction;
    public readonly Solution Solution;
    public readonly EntityUid Uid;
    public List<string> MixingTypes;

    public ReactionAttemptEvent(ReactionSpecification reaction, Solution solution, EntityUid uid, List<string> mixingTypes)
    {
        Reaction = reaction;
        Solution = solution;
        Uid = uid;
        MixingTypes = mixingTypes;
    }
}
