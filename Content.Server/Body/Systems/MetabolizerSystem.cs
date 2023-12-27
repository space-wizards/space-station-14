using Content.Server.Body.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Body.Systems
{
    public sealed class MetabolizerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        private EntityQuery<OrganComponent> _organQuery;
        private EntityQuery<SolutionContainerManagerComponent> _solutionQuery;

        public override void Initialize()
        {
            base.Initialize();

            _organQuery = GetEntityQuery<OrganComponent>();
            _solutionQuery = GetEntityQuery<SolutionContainerManagerComponent>();

            SubscribeLocalEvent<MetabolizerComponent, ComponentInit>(OnMetabolizerInit);
            SubscribeLocalEvent<MetabolizerComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        }

        private void OnMetabolizerInit(EntityUid uid, MetabolizerComponent component, ComponentInit args)
        {
            if (!component.SolutionOnBody)
            {
                _solutionContainerSystem.EnsureSolution(uid, component.SolutionName);
            }
            else if (_organQuery.CompOrNull(uid)?.Body is { } body)
            {
                _solutionContainerSystem.EnsureSolution(body, component.SolutionName);
            }
        }

        private void OnApplyMetabolicMultiplier(EntityUid uid, MetabolizerComponent component,
            ApplyMetabolicMultiplierEvent args)
        {
            if (args.Apply)
            {
                component.UpdateFrequency *= args.Multiplier;
                return;
            }

            component.UpdateFrequency /= args.Multiplier;
            // Reset the accumulator properly
            if (component.AccumulatedFrametime >= component.UpdateFrequency)
                component.AccumulatedFrametime = component.UpdateFrequency;
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
                metab.AccumulatedFrametime += frameTime;

                // Only update as frequently as it should
                if (metab.AccumulatedFrametime < metab.UpdateFrequency)
                    continue;

                metab.AccumulatedFrametime -= metab.UpdateFrequency;
                TryMetabolize(uid, metab);
            }
        }

        private void TryMetabolize(EntityUid uid, MetabolizerComponent meta, OrganComponent? organ = null)
        {
            _organQuery.Resolve(uid, ref organ, false);

            // First step is get the solution we actually care about
            Solution? solution = null;
            EntityUid? solutionEntityUid = null;

            SolutionContainerManagerComponent? manager = null;

            if (meta.SolutionOnBody)
            {
                if (organ?.Body is { } body)
                {
                    if (!_solutionQuery.Resolve(body, ref manager, false))
                        return;

                    _solutionContainerSystem.TryGetSolution(body, meta.SolutionName, out solution, manager);
                    solutionEntityUid = body;
                }
            }
            else
            {
                if (!_solutionQuery.Resolve(uid, ref manager, false))
                    return;

                _solutionContainerSystem.TryGetSolution(uid, meta.SolutionName, out solution, manager);
                solutionEntityUid = uid;
            }

            if (solutionEntityUid == null || solution == null || solution.Contents.Count == 0)
                return;

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
                if (proto.Metabolisms == null)
                {
                    if (meta.RemoveEmpty)
                    {
                        _solutionContainerSystem.RemoveReagent(solutionEntityUid.Value, solution, reagent,
                            FixedPoint2.New(1));
                    }

                    continue;
                }

                // we're done here entirely if this is true
                if (reagents >= meta.MaxReagentsProcessable)
                    return;


                // loop over all our groups and see which ones apply
                if (meta.MetabolismGroups == null)
                    continue;

                foreach (var group in meta.MetabolismGroups)
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
                    if (EntityManager.TryGetComponent<MobStateComponent>(solutionEntityUid.Value, out var state))
                    {
                        if (_mobStateSystem.IsDead(solutionEntityUid.Value, state))
                            continue;
                    }

                    var actualEntity = organ?.Body ?? solutionEntityUid.Value;
                    var args = new ReagentEffectArgs(actualEntity, uid, solution, proto, mostToRemove,
                        EntityManager, null, scale);

                    // do all effects, if conditions apply
                    foreach (var effect in entry.Effects)
                    {
                        if (!effect.ShouldApply(args, _random))
                            continue;

                        if (effect.ShouldLog)
                        {
                            _adminLogger.Add(LogType.ReagentEffect, effect.LogImpact,
                                $"Metabolism effect {effect.GetType().Name:effect} of reagent {proto.LocalizedName:reagent} applied on entity {actualEntity:entity} at {Transform(actualEntity).Coordinates:coordinates}");
                        }

                        effect.Effect(args);
                    }
                }

                // remove a certain amount of reagent
                if (mostToRemove > FixedPoint2.Zero)
                {
                    _solutionContainerSystem.RemoveReagent(solutionEntityUid.Value, solution, reagent, mostToRemove);

                    // We have processed a reagant, so count it towards the cap
                    reagents += 1;
                }
            }
        }
    }

    public sealed class ApplyMetabolicMultiplierEvent : EntityEventArgs
    {
        // The entity whose metabolism is being modified
        public EntityUid Uid;

        // What the metabolism's update rate will be multiplied by
        public float Multiplier;

        // Apply this multiplier or ignore / reset it?
        public bool Apply;
    }
}
