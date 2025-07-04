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

    private void OnAbsorbentSolutionChange(EntityUid uid, AbsorbentComponent component, ref SolutionContainerChangedEvent args)
    {
        if (!SolutionContainer.TryGetSolution(uid, component.SolutionName, out _, out var solution))
            return;

        component.Progress.Clear();

        var mopReagent = solution.GetTotalPrototypeQuantity(Puddle.GetAbsorbentReagents(solution));
        if (mopReagent > FixedPoint2.Zero)
        {
            component.Progress[solution.GetColorWithOnly(_proto, Puddle.GetAbsorbentReagents(solution))] = mopReagent.Float();
        }

        var otherColor = solution.GetColorWithout(_proto, Puddle.GetAbsorbentReagents(solution));
        var other = (solution.Volume - mopReagent).Float();

        if (other > 0f)
        {
            component.Progress[otherColor] = other;
        }

        var remainder = solution.AvailableVolume;

        if (remainder > FixedPoint2.Zero)
        {
            component.Progress[Color.DarkGray] = remainder.Float();
        }
    }
}
