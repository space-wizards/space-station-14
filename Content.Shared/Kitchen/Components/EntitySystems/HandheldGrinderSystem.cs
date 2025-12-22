using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Stacks;

namespace Content.Shared.Kitchen.EntitySystems;

internal sealed class HandheldGrinderSystem : EntitySystem
{
    [Dependency] private readonly SharedReagentGrinderSystem _reagentGrinder = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldGrinderComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HandheldGrinderComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnMapInit(Entity<HandheldGrinderComponent> ent, ref MapInitEvent args)
    {
        if (!_solution.EnsureSolution(ent.Owner, ent.Comp.SolutionName, out var outputSolution))
            return;

        outputSolution.MaxVolume = ent.Comp.SolutionSize;
    }

    private void OnInteractUsing(Entity<HandheldGrinderComponent> ent, ref InteractUsingEvent args)
    {
        Log.Debug("Checking handled.");
        if (args.Handled)
            return;

        Log.Debug("Checking grind.");
        if (ent.Comp.Program == GrinderProgram.Grind && !_reagentGrinder.CanGrind(args.Used))
            return;

        Log.Debug("Checking juice");
        if (ent.Comp.Program == GrinderProgram.Juice && !_reagentGrinder.CanJuice(args.Used))
            return;

        var obtainedSolution = _reagentGrinder.GetGrinderSolution(args.Used, ent.Comp.Program);

        Log.Debug("Checking obtained solution.");
        if (obtainedSolution is null)
            return;

        Log.Debug("Checking output solution exists");
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var outputSolutionEnt, out _))
            return;

        if (TryComp<StackComponent>(args.Used, out var stack))
        {
            Log.Debug("Removing from stack");
            _stackSystem.ReduceCount((args.Used, stack), 1);
        }

        Log.Debug("Adding solution.");
        _solution.TryAddSolution(outputSolutionEnt.Value, obtainedSolution);
    }
}
