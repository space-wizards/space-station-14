using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Smoking;
using System.Linq;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SolutionAreaEffectComponent))]
    public sealed class SmokeSolutionAreaEffectComponent : SolutionAreaEffectComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public new const string SolutionName = "solutionArea";

        protected override void UpdateVisuals()
        {
            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance) &&
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
            {
                appearance.SetData(SmokeVisuals.Color, solution.GetColor());
            }
        }

        protected override void ReactWithEntity(EntityUid entity, double solutionFraction)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
                return;

            if (!_entMan.TryGetComponent(entity, out BloodstreamComponent? bloodstream))
                return;

            if (_entMan.TryGetComponent(entity, out InternalsComponent? internals) &&
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InternalsSystem>().AreInternalsWorking(internals))
                return;

            var chemistry = EntitySystem.Get<ReactiveSystem>();
            var cloneSolution = solution.Clone();
            var transferAmount = FixedPoint2.Min(cloneSolution.CurrentVolume * solutionFraction, bloodstream.ChemicalSolution.AvailableVolume);
            var transferSolution = cloneSolution.SplitSolution(transferAmount);

            foreach (var (id, quantity) in transferSolution.Contents.ToArray())
            {
                chemistry.ReactionEntity(entity, ReactionMethod.Ingestion, id, quantity, transferSolution);
            }

            var bloodstreamSys = EntitySystem.Get<BloodstreamSystem>();
            bloodstreamSys.TryAddToChemicals(entity, transferSolution, bloodstream);
        }


        protected override void OnKill()
        {
            if (_entMan.Deleted(Owner))
                return;
            _entMan.DeleteEntity(Owner);
        }
    }
}
