using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Kitchen.Components;
using Content.Shared.Stacks;

namespace Content.Shared.Kitchen.EntitySystems;

internal sealed class HandheldGrinderSystem : EntitySystem
{
    [Dependency] private readonly SharedReagentGrinderSystem _reagentGrinder = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructibleSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldGrinderComponent, UseInHandEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<HandheldGrinderComponent> ent, ref UseInHandEvent args)
    {
        Log.Debug("Checking handled.");
        if (args.Handled)
            return;

        if (_slots.GetItemOrNull(ent, ent.Comp.ItemSlotName) is not { } item)
            return;

        Log.Debug("Checking grind.");
        if (ent.Comp.Program == GrinderProgram.Grind && !_reagentGrinder.CanGrind(item))
            return;

        Log.Debug("Checking juice");
        if (ent.Comp.Program == GrinderProgram.Juice && !_reagentGrinder.CanJuice(item))
            return;

        args.Handled = true;

        var obtainedSolution = _reagentGrinder.GetGrinderSolution(item, ent.Comp.Program);

        Log.Debug("Checking obtained solution.");
        if (obtainedSolution is null)
            return;

        Log.Debug("Checking output solution exists");
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var outputSolutionEnt, out _))
            return;

        if (TryComp<StackComponent>(item, out var stack))
        {
            Log.Debug("Removing from stack");
            _stackSystem.ReduceCount((item, stack), 1);
            _solution.TryAddSolution(outputSolutionEnt.Value, obtainedSolution);
        }
        else
        {
            Log.Debug("Removing non-stack");
            _solution.TryAddSolution(outputSolutionEnt.Value, obtainedSolution);
            _destructibleSystem.DestroyEntity(item);
        }
    }
}
