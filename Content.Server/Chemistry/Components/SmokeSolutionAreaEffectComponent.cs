using Content.Server.Body.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Smoking;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SolutionAreaEffectComponent))]
    public class SmokeSolutionAreaEffectComponent : SolutionAreaEffectComponent
    {
        public override string Name => "SmokeSolutionAreaEffect";
        public new const string SolutionName = "smoke";

        protected override void UpdateVisuals()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance) &&
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, SolutionName, out var solution))
            {
                appearance.SetData(SmokeVisuals.Color, solution.Color);
            }
        }

        protected override void ReactWithEntity(IEntity entity, double solutionFraction)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, SolutionName, out var solution))
                return;

            if (!entity.TryGetComponent(out BloodstreamComponent? bloodstream))
                return;

            if (entity.TryGetComponent(out InternalsComponent? internals) &&
                internals.AreInternalsWorking())
                return;

            var chemistry = EntitySystem.Get<ChemistrySystem>();
            var cloneSolution = solution.Clone();
            var transferAmount = FixedPoint2.Min(cloneSolution.TotalVolume * solutionFraction, bloodstream.EmptyVolume);
            var transferSolution = cloneSolution.SplitSolution(transferAmount);

            foreach (var reagentQuantity in transferSolution.Contents.ToArray())
            {
                if (reagentQuantity.Quantity == FixedPoint2.Zero) continue;
                chemistry.ReactionEntity(entity.Uid, ReactionMethod.Ingestion, reagentQuantity.ReagentId, reagentQuantity.Quantity, transferSolution);
            }

            bloodstream.TryTransferSolution(transferSolution);
        }


        protected override void OnKill()
        {
            if (Owner.Deleted)
                return;
            Owner.Delete();
        }
    }
}
