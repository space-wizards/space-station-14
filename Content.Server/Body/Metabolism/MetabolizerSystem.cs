using System.Collections.Generic;
using System.Linq;
using Content.Server.Body.Circulatory;
using Content.Server.Chemistry.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Mechanism;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Metabolism
{
    // TODO mirror move updating here to BodySystem so it can be ordered?
    public class MetabolizerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var metab in ComponentManager.EntityQuery<MetabolizerComponent>(false))
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
            var reagentList = new List<Solution.ReagentQuantity>();
            SolutionContainerComponent? solution = null;
            SharedBodyComponent? body = null;

            if (owner.TryGetComponent<SharedMechanismComponent>(out var mech))
            {
                // we have a mechanism, so we'll assume we're in a body and can use the bloodstream to metabolize
                body = mech.Body;
                if (body != null)
                {
                    if (body.Owner.TryGetComponent<BloodstreamComponent>(out var bloodstream)
                        && bloodstream.Solution.CurrentVolume >= ReagentUnit.Zero)
                    {
                        solution = bloodstream.Solution;
                        reagentList = bloodstream.Solution.ReagentList.ToList();
                    }
                }
            }
            else if (owner.TryGetComponent<SolutionContainerComponent>(out var sol))
            {
                // if we have no mechanism/body but a solution container instead,
                // we'll just use that to metabolize from
                solution = sol;
                reagentList = sol.ReagentList.ToList();
            }
            if (solution == null)
            {
                // We're all outta ideas on where to metabolize from
                return;
            }

                // Run metabolism for each reagent, remove metabolized reagents
            // Using ToList here lets us edit reagents while iterating
            foreach (var reagent  in reagentList)
            {
                if (!comp.Metabolisms.ContainsKey(reagent.ReagentId))
                    return;

                var metabolism = comp.Metabolisms[reagent.ReagentId];
                // Run metabolism code for each reagent
                foreach (var effect in metabolism.Effects)
                {
                    // If we're part of a body, pass that entity to Metabolize
                    // Otherwise, just pass our current entity, maybe we're a plant or something
                    effect.Metabolize(body != null ? body.Owner : owner, reagent.Quantity);
                    solution.TryRemoveReagent(reagent.ReagentId, metabolism.MetabolismRate);
                }

                solution.TryRemoveReagent(reagent.ReagentId, metabolism.MetabolismRate);
            }
        }
    }
}
