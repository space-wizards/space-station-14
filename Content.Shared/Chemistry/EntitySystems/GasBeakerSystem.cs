
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed class GasBeakerSystem : EntitySystem
{

    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasBeakerComponent, EntInsertedIntoContainerMessage>(OnTankInserted);
    }

    private void OnTankInserted(EntityUid uid, GasBeakerComponent beaker, EntInsertedIntoContainerMessage args)
    {
        if (!beaker.Initialized)
            return;

        if (args.Container.ID != beaker.TankSlotId)
            return;

        if(!_solutionContainerSystem.TryGetSolution(uid, "beaker", out var solution))
            return;
        if(solution == null)
            return;

        _solutionContainerSystem.UpdateChemicals(uid, solution, true);
    }

}