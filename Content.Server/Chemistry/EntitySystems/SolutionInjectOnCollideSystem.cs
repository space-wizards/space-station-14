using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Projectiles;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionInjectOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainersSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SolutionInjectOnCollideComponent, ProjectileHitEvent>(HandleInjection);
    }

    private void HandleInjection(Entity<SolutionInjectOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        var component = ent.Comp;
        var target = args.Target;

        if (!TryComp<BloodstreamComponent>(target, out var bloodstream) ||
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

        // Make sure 'TryAddToChemicals' of 'bloodstream' will not fail if we try to inject more than it has 'AvailableVolume'
        var validSolutionAmount = component.TransferAmount;
        if (bloodstream.ChemicalSolution != null)
        {
            var availableVolume = bloodstream.ChemicalSolution.Value.Comp.Solution.AvailableVolume;
            if (validSolutionAmount > availableVolume)
                validSolutionAmount = availableVolume;
        }

        var solRemoved = _solutionContainersSystem.SplitSolution(solution.Value, validSolutionAmount);
        var solRemovedVol = solRemoved.Volume;

        var solToInject = solRemoved.SplitSolution(solRemovedVol * component.TransferEfficiency);

        _bloodstreamSystem.TryAddToChemicals(target, solToInject, bloodstream);
    }
}
