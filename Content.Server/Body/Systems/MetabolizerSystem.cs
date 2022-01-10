using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Body.Systems
{
    [UsedImplicitly]
    public class MetabolizerSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAdminLogSystem _logSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MetabolizerComponent, ComponentInit>(OnMetabolizerInit);
        }

        private void OnMetabolizerInit(EntityUid uid, MetabolizerComponent component, ComponentInit args)
        {
            if (!component.SolutionOnBody)
            {
                _solutionContainerSystem.EnsureSolution(uid, component.SolutionName);
            }
            else
            {
                if (EntityManager.TryGetComponent<MechanismComponent>(uid, out var mech))
                {
                    if (mech.Body != null)
                    {
                        _solutionContainerSystem.EnsureSolution((mech.Body).Owner, component.SolutionName);
                    }
                }
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var metab in EntityManager.EntityQuery<MetabolizerComponent>(false))
            {
                metab.AccumulatedFrametime += frameTime;

                // Only update as frequently as it should
                if (metab.AccumulatedFrametime >= metab.UpdateFrequency)
                {
                    metab.AccumulatedFrametime -= metab.UpdateFrequency;
                    TryMetabolize((metab).Owner, metab);
                }
            }
        }

        private void TryMetabolize(EntityUid uid, MetabolizerComponent? meta=null, MechanismComponent? mech=null)
        {
            if (!Resolve(uid, ref meta))
                return;

            Resolve(uid, ref mech, false);

            // First step is get the solution we actually care about
            Solution? solution = null;
            EntityUid? solutionEntityUid = null;
            EntityUid? bodyEntityUid = mech?.Body?.Owner;

            SolutionContainerManagerComponent? manager = null;

            if (meta.SolutionOnBody)
            {
                if (mech != null)
                {
                    var body = mech.Body;

                    if (body != null)
                    {
                        if (!Resolve((body).Owner, ref manager, false))
                            return;
                        _solutionContainerSystem.TryGetSolution((body).Owner, meta.SolutionName, out solution, manager);
                        solutionEntityUid = body.Owner;
                    }
                }
            }
            else
            {
                if (!Resolve(uid, ref manager, false))
                    return;
                _solutionContainerSystem.TryGetSolution(uid, meta.SolutionName, out solution, manager);
                solutionEntityUid = uid;
            }

            if (solutionEntityUid == null || solution == null)
                return;

            // randomize the reagent list so we don't have any weird quirks
            // like alphabetical order or insertion order mattering for processing
            var list = solution.Contents.ToArray();
            _random.Shuffle(list);

            int reagents = 0;
            foreach (var reagent in list)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.ReagentId, out var proto))
                    continue;

                FixedPoint2 mostToRemove = FixedPoint2.Zero;
                if (proto.Metabolisms == null)
                {
                    if (meta.RemoveEmpty)
                        _solutionContainerSystem.TryRemoveReagent(solutionEntityUid.Value, solution, reagent.ReagentId, FixedPoint2.New(1));
                    continue;
                }

                // we're done here entirely if this is true
                if (reagents >= meta.MaxReagentsProcessable)
                    return;
                reagents += 1;

                // loop over all our groups and see which ones apply
                if (meta.MetabolismGroups == null)
                    continue;

                foreach (var group in meta.MetabolismGroups)
                {
                    if (!proto.Metabolisms.Keys.Contains(group.Id))
                        continue;

                    var entry = proto.Metabolisms[group.Id];

                    // we don't remove reagent for every group, just whichever had the biggest rate
                    if (entry.MetabolismRate > mostToRemove)
                        mostToRemove = entry.MetabolismRate;

                    mostToRemove *= group.MetabolismRateModifier;

                    mostToRemove = FixedPoint2.Clamp(mostToRemove, 0, reagent.Quantity);

                    // if it's possible for them to be dead, and they are,
                    // then we shouldn't process any effects, but should probably
                    // still remove reagents
                    if (EntityManager.TryGetComponent<MobStateComponent>(solutionEntityUid.Value, out var state))
                    {
                        if (state.IsDead())
                            continue;
                    }

                    var actualEntity = bodyEntityUid != null ? bodyEntityUid.Value : solutionEntityUid.Value;
                    var args = new ReagentEffectArgs(actualEntity, (meta).Owner, solution, proto, mostToRemove,
                        EntityManager, null);

                    // do all effects, if conditions apply
                    foreach (var effect in entry.Effects)
                    {
                        if (!effect.ShouldApply(args, _random))
                            continue;

                        if (effect.ShouldLog)
                        {
                            _logSystem.Add(LogType.ReagentEffect, effect.LogImpact,
                                $"Metabolism effect {effect.GetType().Name:effect} of reagent {args.Reagent.Name:reagent} applied on entity {actualEntity:entity} at {Transform(actualEntity).Coordinates:coordinates}");
                        }

                        effect.Effect(args);
                    }
                }

                // remove a certain amount of reagent
                if (mostToRemove > FixedPoint2.Zero)
                    _solutionContainerSystem.TryRemoveReagent(solutionEntityUid.Value, solution, reagent.ReagentId, mostToRemove);
            }
        }
    }
}
