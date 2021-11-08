using System.Collections.Generic;
using Content.Server.Body.Circulatory;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Mechanism;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Body.Metabolism
{
    // TODO mirror in the future working on mechanisms move updating here to BodySystem so it can be ordered?
    [UsedImplicitly]
    public class MetabolizerSystem : EntitySystem
    {
        [Dependency]
        private readonly SolutionContainerSystem _solutionContainerSystem = default!;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MetabolizerComponent, ComponentInit>(OnMetabolizerInit);
        }

        private void OnMetabolizerInit(EntityUid uid, MetabolizerComponent component, ComponentInit args)
        {
            _solutionContainerSystem.EnsureSolution(uid, component.SolutionName);
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
                    metab.AccumulatedFrametime = 0.0f;
                    TryMetabolize(metab);
                }
            }
        }

        private void TryMetabolize(MetabolizerComponent comp)
        {
            var owner = comp.Owner;
            IReadOnlyList<Solution.ReagentQuantity> reagentList = new List<Solution.ReagentQuantity>();
            Solution? solution = null;
            SharedBodyComponent? body = null;

            // if this field is passed we should try and take from the bloodstream over anything else
            if (owner.TryGetComponent<SharedMechanismComponent>(out var mech))
            {
                body = mech.Body;
                if (body != null)
                {
                    if (body.Owner.HasComponent<BloodstreamComponent>()
                        && _solutionContainerSystem.TryGetSolution(body.OwnerUid, comp.SolutionName, out solution)
                        && solution.CurrentVolume >= FixedPoint2.Zero)
                    {
                        reagentList = solution.Contents;
                    }
                }
            }

            if (solution == null || reagentList.Count == 0)
            {
                // We're all outta ideas on where to metabolize from
                return;
            }

            List<Solution.ReagentQuantity> removeReagents = new(5);
            var ent = body?.Owner ?? owner;

            // Run metabolism for each reagent, remove metabolized reagents
            foreach (var reagent in reagentList)
            {
                if (!comp.Metabolisms.ContainsKey(reagent.ReagentId))
                    continue;

                var metabolism = comp.Metabolisms[reagent.ReagentId];
                // Run metabolism code for each reagent
                foreach (var effect in metabolism.Effects)
                {
                    var conditionsMet = true;
                    if (effect.Conditions != null)
                    {
                        // yes this is 3 nested for loops, but all of these lists are
                        // basically guaranteed to be small or empty
                        foreach (var condition in effect.Conditions)
                        {
                            if (!condition.Condition(ent, reagent))
                            {
                                conditionsMet = false;
                                break;
                            }
                        }
                    }

                    if (!conditionsMet)
                        continue;

                    // If we're part of a body, pass that entity to Metabolize
                    // Otherwise, just pass our owner entity, maybe we're a plant or something
                    effect.Metabolize(ent, reagent);
                }

                removeReagents.Add(new Solution.ReagentQuantity(reagent.ReagentId, metabolism.MetabolismRate));
            }

            _solutionContainerSystem.TryRemoveAllReagents(ent.Uid, solution, removeReagents);
        }
    }
}
