using Content.Server.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems
{
    public sealed class MetabolizerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

        private EntityQuery<OrganComponent> _organQuery;
        private EntityQuery<SolutionContainerManagerComponent> _solutionQuery;

        public override void Initialize()
        {
            base.Initialize();

            _organQuery = GetEntityQuery<OrganComponent>();
            _solutionQuery = GetEntityQuery<SolutionContainerManagerComponent>();

            SubscribeLocalEvent<MetabolizerComponent, ComponentInit>(OnMetabolizerInit);
            SubscribeLocalEvent<MetabolizerComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<MetabolizerComponent, EntityUnpausedEvent>(OnUnpaused);
            SubscribeLocalEvent<MetabolizerComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        }

        private void OnMapInit(Entity<MetabolizerComponent> ent, ref MapInitEvent args)
        {
            ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval;
        }

        private void OnUnpaused(Entity<MetabolizerComponent> ent, ref EntityUnpausedEvent args)
        {
            ent.Comp.NextUpdate += args.PausedTime;
        }

        private void OnMetabolizerInit(Entity<MetabolizerComponent> entity, ref ComponentInit args)
        {
            if (!entity.Comp.SolutionOnBody)
            {
                _solutionContainerSystem.EnsureSolution(entity.Owner, entity.Comp.SolutionName, out _);
            }
            else if (_organQuery.CompOrNull(entity)?.Body is { } body)
            {
                _solutionContainerSystem.EnsureSolution(body, entity.Comp.SolutionName, out _);
            }
        }

        private void OnApplyMetabolicMultiplier(
            Entity<MetabolizerComponent> ent,
            ref ApplyMetabolicMultiplierEvent args)
        {
            // TODO REFACTOR THIS
            // This will slowly drift over time due to floating point errors.
            // Instead, raise an event with the base rates and allow modifiers to get applied to it.
            if (args.Apply)
            {
                ent.Comp.UpdateInterval *= args.Multiplier;
                return;
            }

            ent.Comp.UpdateInterval /= args.Multiplier;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var metabolizers = new ValueList<(EntityUid Uid, MetabolizerComponent Component)>(Count<MetabolizerComponent>());
            var query = EntityQueryEnumerator<MetabolizerComponent>();

            while (query.MoveNext(out var uid, out var comp))
            {
                metabolizers.Add((uid, comp));
            }

            foreach (var (uid, metab) in metabolizers)
            {
                // Only update as frequently as it should
                if (_gameTiming.CurTime < metab.NextUpdate)
                    continue;

                metab.NextUpdate += metab.UpdateInterval;
                TryMetabolize((uid, metab));
            }
        }

        private void TryMetabolize(Entity<MetabolizerComponent, OrganComponent?, SolutionContainerManagerComponent?> ent)
        {
            _organQuery.Resolve(ent, ref ent.Comp2, logMissing: false);

            // First step is get the solution we actually care about
            var solutionName = ent.Comp1.SolutionName;
            Solution? solution = null;
            Entity<SolutionComponent>? soln = default!;
            EntityUid? solutionEntityUid = null;

            if (ent.Comp1.SolutionOnBody)
            {
                if (ent.Comp2?.Body is { } body)
                {
                    if (!_solutionQuery.Resolve(body, ref ent.Comp3, logMissing: false))
                        return;

                    _solutionContainerSystem.TryGetSolution((body, ent.Comp3), solutionName, out soln, out solution);
                    solutionEntityUid = body;
                }
            }
            else
            {
                if (!_solutionQuery.Resolve(ent, ref ent.Comp3, logMissing: false))
                    return;

                _solutionContainerSystem.TryGetSolution((ent, ent), solutionName, out soln, out solution);
                solutionEntityUid = ent;
            }

            if (solutionEntityUid is null
                || soln is null
                || solution is null
                || solution.Contents.Count == 0)
            {
                return;
            }

            // randomize the reagent list so we don't have any weird quirks
            // like alphabetical order or insertion order mattering for processing
            var list = solution.Contents.ToArray();
            _random.Shuffle(list);

            int reagents = 0;
            foreach (var (reagent, quantity) in list)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Prototype, out var proto))
                    continue;

                var mostToRemove = FixedPoint2.Zero;
                if (proto.Metabolisms is null)
                {
                    if (ent.Comp1.RemoveEmpty)
                    {
                        solution.RemoveReagent(reagent, FixedPoint2.New(1));
                    }

                    continue;
                }

                // we're done here entirely if this is true
                if (reagents >= ent.Comp1.MaxReagentsProcessable)
                    return;


                // loop over all our groups and see which ones apply
                if (ent.Comp1.MetabolismGroups is null)
                    continue;

                foreach (var group in ent.Comp1.MetabolismGroups)
                {
                    if (!proto.Metabolisms.TryGetValue(group.Id, out var entry))
                        continue;

                    var rate = entry.MetabolismRate * group.MetabolismRateModifier;

                    // Remove $rate, as long as there's enough reagent there to actually remove that much
                    mostToRemove = FixedPoint2.Clamp(rate, 0, quantity);

                    float scale = (float) mostToRemove / (float) rate;

                    // if it's possible for them to be dead, and they are,
                    // then we shouldn't process any effects, but should probably
                    // still remove reagents
                    if (TryComp<MobStateComponent>(solutionEntityUid.Value, out var state))
                    {
                        if (!proto.WorksOnTheDead && _mobStateSystem.IsDead(solutionEntityUid.Value, state))
                            continue;
                    }

                    var actualEntity = ent.Comp2?.Body ?? solutionEntityUid.Value;
                    var args = new EntityEffectReagentArgs(actualEntity, EntityManager, ent, solution, mostToRemove, proto, null, scale);

                    // do all effects, if conditions apply
                    foreach (var effect in entry.Effects)
                    {
                        if (!effect.ShouldApply(args, _random))
                            continue;

                        if (effect.ShouldLog)
                        {
                            _adminLogger.Add(
                                LogType.ReagentEffect,
                                effect.LogImpact,
                                $"Metabolism effect {effect.GetType().Name:effect}"
                                + $" of reagent {proto.LocalizedName:reagent}"
                                + $" applied on entity {actualEntity:entity}"
                                + $" at {Transform(actualEntity).Coordinates:coordinates}"
                            );
                        }

                        effect.Effect(args);
                    }
                }

                // remove a certain amount of reagent
                if (mostToRemove > FixedPoint2.Zero)
                {
                    solution.RemoveReagent(reagent, mostToRemove);

                    // We have processed a reagant, so count it towards the cap
                    reagents += 1;
                }
            }

            _solutionContainerSystem.UpdateChemicals(soln.Value);
        }
    }

    // TODO REFACTOR THIS
    // This will cause rates to slowly drift over time due to floating point errors.
    // Instead, the system that raised this should trigger an update and subscribe to get-modifier events.
    [ByRefEvent]
    public readonly record struct ApplyMetabolicMultiplierEvent(
        EntityUid Uid,
        float Multiplier,
        bool Apply)
    {
        /// <summary>
        /// The entity whose metabolism is being modified.
        /// </summary>
        public readonly EntityUid Uid = Uid;

        /// <summary>
        /// What the metabolism's update rate will be multiplied by.
        /// </summary>
        public readonly float Multiplier = Multiplier;

        /// <summary>
        /// If true, apply the multiplier. If false, revert it.
        /// </summary>
        public readonly bool Apply = Apply;
    }
}
