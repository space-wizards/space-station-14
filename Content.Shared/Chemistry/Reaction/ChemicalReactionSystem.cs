using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction
{
    public sealed class ChemicalReactionSystem : EntitySystem
    {
        /// <summary>
        ///     The maximum number of reactions that may occur when a solution is changed.
        /// </summary>
        private const int MaxReactionIterations = 20;

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        /// <summary>
        ///     A cache of all existant chemical reactions indexed by one of their
        ///     required reactants.
        /// </summary>
        private IDictionary<string, List<ReactionPrototype>> _reactions = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeReactionCache();
            _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _prototypeManager.PrototypesReloaded -= OnPrototypesReloaded;
        }

        /// <summary>
        ///     Handles building the reaction cache.
        /// </summary>
        private void InitializeReactionCache()
        {
            _reactions = new Dictionary<string, List<ReactionPrototype>>();

            var reactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>();
            foreach(var reaction in reactions)
            {
                CacheReaction(reaction);
            }
        }

        /// <summary>
        ///     Caches a reaction by its first required reagent.
        ///     Used to build the reaction cache.
        /// </summary>
        /// <param name="reaction">A reaction prototype to cache.</param>
        private void CacheReaction(ReactionPrototype reaction)
        {
            var reagents = reaction.Reactants.Keys;
            foreach(var reagent in reagents)
            {
                if(!_reactions.TryGetValue(reagent, out var cache))
                {
                    cache = new List<ReactionPrototype>();
                    _reactions.Add(reagent, cache);
                }

                cache.Add(reaction);
                return; // Only need to cache based on the first reagent.
            }
        }

        /// <summary>
        ///     Updates the reaction cache when the prototypes are reloaded.
        /// </summary>
        /// <param name="eventArgs">The set of modified prototypes.</param>
        private void OnPrototypesReloaded(PrototypesReloadedEventArgs eventArgs)
        {
            if (!eventArgs.ByType.TryGetValue(typeof(ReactionPrototype), out var set))
                return;

            foreach (var (reactant, cache) in _reactions)
            {
                cache.RemoveAll((reaction) => set.Modified.ContainsKey(reaction.ID));
                if (cache.Count == 0)
                    _reactions.Remove(reactant);
            }

            foreach (var prototype in set.Modified.Values)
            {
                CacheReaction((ReactionPrototype) prototype);
            }
        }

        /// <summary>
        ///     Checks if a solution can undergo a specified reaction.
        /// </summary>
        /// <param name="solution">The solution to check.</param>
        /// <param name="reaction">The reaction to check.</param>
        /// <param name="lowestUnitReactions">How many times this reaction can occur.</param>
        /// <returns></returns>
        private bool CanReact(Solution solution, ReactionPrototype reaction, EntityUid owner, ReactionMixerComponent? mixerComponent, out FixedPoint2 lowestUnitReactions)
        {
            lowestUnitReactions = FixedPoint2.MaxValue;
            if (solution.Temperature < reaction.MinimumTemperature)
            {
                lowestUnitReactions = FixedPoint2.Zero;
                return false;
            } else if(solution.Temperature > reaction.MaximumTemperature)
            {
                lowestUnitReactions = FixedPoint2.Zero;
                return false;
            }

            if((mixerComponent == null && reaction.MixingCategories != null) ||
                mixerComponent != null && reaction.MixingCategories != null && reaction.MixingCategories.Except(mixerComponent.ReactionTypes).Any())
            {
                lowestUnitReactions = FixedPoint2.Zero;
                return false;
            }

            var attempt = new ReactionAttemptEvent(reaction, solution);
            RaiseLocalEvent(owner, attempt, false);
            if (attempt.Cancelled)
            {
                lowestUnitReactions = FixedPoint2.Zero;
                return false;
            }

            foreach (var reactantData in reaction.Reactants)
            {
                var reactantName = reactantData.Key;
                var reactantCoefficient = reactantData.Value.Amount;

                var reactantQuantity = solution.GetTotalPrototypeQuantity(reactantName);

                if (reactantQuantity <= FixedPoint2.Zero)
                    return false;

                if (reactantData.Value.Catalyst)
                {
                    // catalyst is not consumed, so will not limit the reaction. But it still needs to be present, and
                    // for quantized reactions we need to have a minimum amount

                    if (reactantQuantity == FixedPoint2.Zero || reaction.Quantized && reactantQuantity < reactantCoefficient)
                        return false;

                    continue;
                }

                var unitReactions = reactantQuantity / reactantCoefficient;

                if (unitReactions < lowestUnitReactions)
                {
                    lowestUnitReactions = unitReactions;
                }
            }

            if (reaction.Quantized)
                lowestUnitReactions = (int) lowestUnitReactions;

            return lowestUnitReactions > 0;
        }

        /// <summary>
        ///     Perform a reaction on a solution. This assumes all reaction criteria are met.
        ///     Removes the reactants from the solution, adds products, and returns a list of products.
        /// </summary>
        private List<string> PerformReaction(Solution solution, EntityUid owner, ReactionPrototype reaction, FixedPoint2 unitReactions)
        {
            var energy = reaction.ConserveEnergy ? solution.GetThermalEnergy(_prototypeManager) : 0;

            //Remove reactants
            foreach (var reactant in reaction.Reactants)
            {
                if (!reactant.Value.Catalyst)
                {
                    var amountToRemove = unitReactions * reactant.Value.Amount;
                    solution.RemoveReagent(reactant.Key, amountToRemove);
                }
            }

            //Create products
            var products = new List<string>();
            foreach (var product in reaction.Products)
            {
                products.Add(product.Key);
                solution.AddReagent(product.Key, product.Value * unitReactions);
            }

            if (reaction.ConserveEnergy)
            {
                var newCap = solution.GetHeatCapacity(_prototypeManager);
                if (newCap > 0)
                    solution.Temperature = energy / newCap;
            }

            OnReaction(solution, reaction, null, owner, unitReactions);

            return products;
        }

        private void OnReaction(Solution solution, ReactionPrototype reaction, ReagentPrototype? reagent, EntityUid owner, FixedPoint2 unitReactions)
        {
            var args = new ReagentEffectArgs(owner, null, solution,
                reagent,
                unitReactions, EntityManager, null, 1f);

            var coordinates = Transform(owner).Coordinates;
            _adminLogger.Add(LogType.ChemicalReaction, reaction.Impact,
                $"Chemical reaction {reaction.ID:reaction} occurred with strength {unitReactions:strength} on entity {ToPrettyString(owner):metabolizer} at {coordinates}");

            foreach (var effect in reaction.Effects)
            {
                if (!effect.ShouldApply(args))
                    continue;

                if (effect.ShouldLog)
                {
                    var entity = args.SolutionEntity;
                    _adminLogger.Add(LogType.ReagentEffect, effect.LogImpact,
                        $"Reaction effect {effect.GetType().Name:effect} of reaction ${reaction.ID:reaction} applied on entity {ToPrettyString(entity):entity} at {Transform(entity).Coordinates:coordinates}");
                }

                effect.Effect(args);
            }

            _audio.PlayPvs(reaction.Sound, owner);
        }

        /// <summary>
        ///     Performs all chemical reactions that can be run on a solution.
        ///     Removes the reactants from the solution, then returns a solution with all products.
        ///     WARNING: Does not trigger reactions between solution and new products.
        /// </summary>
        private bool ProcessReactions(Solution solution, EntityUid owner, FixedPoint2 maxVolume, SortedSet<ReactionPrototype> reactions, ReactionMixerComponent? mixerComponent)
        {
            HashSet<ReactionPrototype> toRemove = new();
            List<string>? products = null;

            // attempt to perform any applicable reaction
            foreach (var reaction in reactions)
            {
                if (!CanReact(solution, reaction, owner, mixerComponent, out var unitReactions))
                {
                    toRemove.Add(reaction);
                    continue;
                }

                products = PerformReaction(solution, owner, reaction, unitReactions);
                break;
            }

            // did any reaction occur?
            if (products == null)
                return false;

            if (products.Count == 0)
                return true;

            // Add any reactions associated with the new products. This may re-add reactions that were already iterated
            // over previously. The new product may mean the reactions are applicable again and need to be processed.
            foreach (var product in products)
            {
                if (_reactions.TryGetValue(product, out var reactantReactions))
                    reactions.UnionWith(reactantReactions);
            }

            return true;
        }

        /// <summary>
        ///     Continually react a solution until no more reactions occur, with a volume constraint.
        /// </summary>
        public void FullyReactSolution(Solution solution, EntityUid owner, FixedPoint2 maxVolume, ReactionMixerComponent? mixerComponent = null)
        {
            // construct the initial set of reactions to check.
            SortedSet<ReactionPrototype> reactions = new();
            foreach (var reactant in solution.Contents)
            {
                if (_reactions.TryGetValue(reactant.Reagent.Prototype, out var reactantReactions))
                    reactions.UnionWith(reactantReactions);
            }

            // Repeatedly attempt to perform reactions, ending when there are no more applicable reactions, or when we
            // exceed the iteration limit.
            for (var i = 0; i < MaxReactionIterations; i++)
            {
                if (!ProcessReactions(solution, owner, maxVolume, reactions, mixerComponent))
                    return;
            }

            Logger.Error($"{nameof(Solution)} {owner} could not finish reacting in under {MaxReactionIterations} loops.");
        }
    }

    /// <summary>
    ///     Raised directed at the owner of a solution to determine whether the reaction should be allowed to occur.
    /// </summary>
    /// <reamrks>
    ///     Some solution containers (e.g., bloodstream, smoke, foam) use this to block certain reactions from occurring.
    /// </reamrks>
    public sealed class ReactionAttemptEvent : CancellableEntityEventArgs
    {
        public readonly ReactionPrototype Reaction;
        public readonly Solution Solution;

        public ReactionAttemptEvent(ReactionPrototype reaction, Solution solution)
        {
            Reaction = reaction;
            Solution = solution;
        }
    }
}
