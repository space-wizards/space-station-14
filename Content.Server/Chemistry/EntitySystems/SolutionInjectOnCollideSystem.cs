using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Shared.Physics.Events;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionInjectOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainersSystem = default!;
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
            !args.OurBody.Hard ||
            !EntityManager.TryGetComponent<BloodstreamComponent>(target, out var bloodstream) ||
            !_solutionContainersSystem.TryGetInjectableSolution(ent.Owner, out var solution, out _))
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

        var solRemoved = _solutionContainersSystem.SplitSolution(solution.Value, component.TransferAmount);
        var solRemovedVol = solRemoved.Volume;

        var solToInject = solRemoved.SplitSolution(solRemovedVol * component.TransferEfficiency);

        _bloodstreamSystem.TryAddToChemicals(target, solToInject, bloodstream);
    }
}
