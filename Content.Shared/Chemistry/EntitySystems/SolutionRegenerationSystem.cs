using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed class SolutionRegenerationSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionRegenerationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SolutionRegenerationComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnMapInit(Entity<SolutionRegenerationComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextRegenTime = _timing.CurTime + ent.Comp.Duration;

        // Okay, so I cannot figure out a way to get around networking this—without it, the predicted tick where the
        // solution gets updated ends up being too early, causing really annoying mispredicts in the Absorption UI,
        // where the water bar flutters back and forth.
        Dirty(ent);
    }

    // Workaround for https://github.com/space-wizards/space-station-14/pull/35314
    private void OnEntRemoved(Entity<SolutionRegenerationComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity was our contained solution
        if (ent.Comp is not { SolutionRef: { } sol }
            || args.Entity != sol.Owner)
            return;

        // Clear our cached reference to the solution entity
        ent.Comp.SolutionRef = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SolutionRegenerationComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var regen, out var manager))
        {
            if (_timing.CurTime < regen.NextRegenTime)
                continue;

            // timer ignores if its full, it's just a fixed cycle
            regen.NextRegenTime += regen.Duration;
            Dirty(uid, regen);
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
