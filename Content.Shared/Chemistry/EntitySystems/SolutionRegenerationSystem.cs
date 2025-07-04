using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed class SolutionRegenerationSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SolutionRegenerationComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var regen, out var manager))
        {
            if (_timing.CurTime < regen.NextRegenTime)
                continue;

            // timer ignores if its full, it's just a fixed cycle
            regen.NextRegenTime = _timing.CurTime + regen.Duration;
            if (!_solutionContainer.ResolveSolution((uid, manager),
                    regen.SolutionName,
                    ref regen.SolutionRef,
                    out var solution))
                continue;

            var amount = FixedPoint2.Min(solution.AvailableVolume, regen.Generated.Volume);
            if (amount <= FixedPoint2.Zero)
                continue;

            // Don't bother cloning and splitting if adding the whole thing
            var generated = amount == regen.Generated.Volume
                ? regen.Generated
                : regen.Generated.Clone().SplitSolution(amount);

            _solutionContainer.TryAddSolution(regen.SolutionRef.Value, generated);
        }
    }
}
