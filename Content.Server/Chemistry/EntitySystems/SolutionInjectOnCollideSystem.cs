using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    internal sealed class SolutionInjectOnCollideSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SolutionInjectOnCollideComponent, StartCollideEvent>(HandleInjection);
        }

        private void HandleInjection(Entity<SolutionInjectOnCollideComponent> ent, ref StartCollideEvent args)
        {
            var component = ent.Comp;
            var target = args.OtherEntity;

            if (!args.OtherBody.Hard ||
                !EntityManager.TryGetComponent<BloodstreamComponent>(target, out var bloodstream) ||
                !_solutionsSystem.TryGetInjectableSolution(ent, out var solution))
            {
                return;
            }

            if (component.BlockSlots != 0x0)
            {
                var containerEnumerator = _inventorySystem.GetSlotEnumerator(target, component.BlockSlots);

                // TODO add a helper method for this?
                if (containerEnumerator.MoveNext(out _))
                    return;
            }

            var solRemoved = solution.SplitSolution(component.TransferAmount);
            var solRemovedVol = solRemoved.Volume;

            var solToInject = solRemoved.SplitSolution(solRemovedVol * component.TransferEfficiency);

            _bloodstreamSystem.TryAddToChemicals(target, solToInject, bloodstream);
        }
    }
}
