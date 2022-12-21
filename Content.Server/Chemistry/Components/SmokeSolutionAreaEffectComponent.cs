using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Smoking;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SolutionAreaEffectComponent))]
    public sealed class SmokeSolutionAreaEffectComponent : SolutionAreaEffectComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        public new const string SolutionName = "solutionArea";

        protected override void UpdateVisuals()
        {
            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance) &&
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
            {
                appearance.SetData(SmokeVisuals.Color, solution.Color);
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
            var transferAmount = FixedPoint2.Min(cloneSolution.TotalVolume * solutionFraction, bloodstream.ChemicalSolution.AvailableVolume);
            var transferSolution = cloneSolution.SplitSolution(transferAmount);

            foreach (var reagentQuantity in transferSolution.Contents.ToArray())
            {
                if (reagentQuantity.Quantity == FixedPoint2.Zero) continue;
                chemistry.ReactionEntity(entity, ReactionMethod.Ingestion, reagentQuantity.ReagentId, reagentQuantity.Quantity, transferSolution);
            }

            var bloodstreamSys = EntitySystem.Get<BloodstreamSystem>();
            if (bloodstreamSys.TryAddToChemicals(entity, transferSolution, bloodstream))
            {
                // Log solution addition by smoke
                _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{_entMan.ToPrettyString(entity):target} was affected by smoke {SolutionContainerSystem.ToPrettyString(transferSolution)}");
            }
        }


        protected override void OnKill()
        {
            if (_entMan.Deleted(Owner))
                return;
            _entMan.DeleteEntity(Owner);
        }
    }
}
