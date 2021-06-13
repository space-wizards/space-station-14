#nullable enable
using System.Linq;
using Content.Server.Body.Circulatory;
using Content.Server.Body.Respiratory;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
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

        protected override void UpdateVisuals()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance) &&
                SolutionContainerComponent != null)
            {
                appearance.SetData(SmokeVisuals.Color, SolutionContainerComponent.Color);
            }
        }

        protected override void ReactWithEntity(IEntity entity, double solutionFraction)
        {
            if (SolutionContainerComponent == null)
                return;

            if (!entity.TryGetComponent(out BloodstreamComponent? bloodstream))
                return;

            if (entity.TryGetComponent(out InternalsComponent? internals) &&
                internals.AreInternalsWorking())
                return;

            var chemistry = EntitySystem.Get<ChemistrySystem>();
            var cloneSolution = SolutionContainerComponent.Solution.Clone();
            var transferAmount = ReagentUnit.Min(cloneSolution.TotalVolume * solutionFraction, bloodstream.EmptyVolume);
            var transferSolution = cloneSolution.SplitSolution(transferAmount);

            foreach (var reagentQuantity in transferSolution.Contents.ToArray())
            {
                if (reagentQuantity.Quantity == ReagentUnit.Zero) continue;
                chemistry.ReactionEntity(entity, ReactionMethod.Ingestion, reagentQuantity.ReagentId, reagentQuantity.Quantity, transferSolution);
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
