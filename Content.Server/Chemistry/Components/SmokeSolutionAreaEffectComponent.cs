using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Smoking;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SolutionAreaEffectComponent))]
    public class SmokeSolutionAreaEffectComponent : SolutionAreaEffectComponent
    {
        public override string Name => "SmokeSolutionAreaEffect";
        public new const string SolutionName = "solutionArea";

        protected override void UpdateVisuals()
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner.Uid, out AppearanceComponent? appearance) &&
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, SolutionName, out var solution))
            {
                appearance.SetData(SmokeVisuals.Color, solution.Color);
            }
        }

        protected override void ReactWithEntity(IEntity entity, double solutionFraction)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, SolutionName, out var solution))
                return;

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity.Uid, out BloodstreamComponent? bloodstream))
                return;

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity.Uid, out InternalsComponent? internals) &&
                internals.AreInternalsWorking())
                return;

            var chemistry = EntitySystem.Get<ReactiveSystem>();
            var cloneSolution = solution.Clone();
            var transferAmount = FixedPoint2.Min(cloneSolution.TotalVolume * solutionFraction, bloodstream.Solution.AvailableVolume);
            var transferSolution = cloneSolution.SplitSolution(transferAmount);

            foreach (var reagentQuantity in transferSolution.Contents.ToArray())
            {
                if (reagentQuantity.Quantity == FixedPoint2.Zero) continue;
                chemistry.ReactionEntity(entity.Uid, ReactionMethod.Ingestion, reagentQuantity.ReagentId, reagentQuantity.Quantity, transferSolution);
            }

            var bloodstreamSys = EntitySystem.Get<BloodstreamSystem>();
            bloodstreamSys.TryAddToBloodstream(entity.Uid, transferSolution, bloodstream);
        }


        protected override void OnKill()
        {
            if ((!IoCManager.Resolve<IEntityManager>().EntityExists(Owner.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Uid).EntityLifeStage) >= EntityLifeStage.Deleted)
                return;
            IoCManager.Resolve<IEntityManager>().DeleteEntity(Owner.Uid);
        }
    }
}
