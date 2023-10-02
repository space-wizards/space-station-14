using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;


namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    internal sealed class SolutionPickOnCollideSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SolutionPickOnCollideComponent, ComponentInit>(HandleInit);
            SubscribeLocalEvent<SolutionPickOnCollideComponent, StartCollideEvent>(HandleInjection);
        }

        private void HandleInit(EntityUid uid, SolutionPickOnCollideComponent component, ComponentInit args)
        {
            component.Owner
                .EnsureComponentWarn<SolutionContainerManagerComponent>($"{nameof(SolutionPickOnCollideComponent)} requires a SolutionContainerManager on {component.Owner}!");
        }

        private void HandleInjection(EntityUid uid, SolutionPickOnCollideComponent component, ref StartCollideEvent args)
        {
            var target = args.OtherEntity;

            if (!args.OtherBody.Hard ||
                !EntityManager.TryGetComponent<BloodstreamComponent>(target, out var bloodstream) ||
                !_solutionsSystem.TryGetSolution(uid, component.Solution, out var solution)) return;

            _solutionsSystem.TryTransferSolution(uid, solution, bloodstream.BloodSolution, component.TransferAmount * component.TransferEfficiency);
        }
    }
}
