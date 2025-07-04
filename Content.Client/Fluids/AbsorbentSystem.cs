using Content.Client.Fluids.UI;
using Content.Client.Items;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Robust.Shared.Prototypes;

namespace Content.Client.Fluids;

/// <inheritdoc/>
public sealed class AbsorbentSystem : SharedAbsorbentSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbsorbentComponent, SolutionContainerChangedEvent>(OnAbsorbentSolutionChange);

        Subs.ItemStatus<AbsorbentComponent>(ent => new AbsorbentItemStatus(ent, EntityManager));
    }

    private void OnAbsorbentSolutionChange(Entity<AbsorbentComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (!SolutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out _, out var solution))
            return;

        ent.Comp.Progress.Clear();

        var absorbentReagents = Puddle.GetAbsorbentReagents(solution);
        var mopReagent = solution.GetTotalPrototypeQuantity(absorbentReagents);
        if (mopReagent > FixedPoint2.Zero)
            ent.Comp.Progress[solution.GetColorWithOnly(_proto, absorbentReagents)] = mopReagent.Float();

        var otherColor = solution.GetColorWithout(_proto, absorbentReagents);
        var other = solution.Volume - mopReagent;
        if (other > FixedPoint2.Zero)
            ent.Comp.Progress[otherColor] = other.Float();

        if (solution.AvailableVolume > FixedPoint2.Zero)
            ent.Comp.Progress[Color.DarkGray] = solution.AvailableVolume.Float();
    }
}
