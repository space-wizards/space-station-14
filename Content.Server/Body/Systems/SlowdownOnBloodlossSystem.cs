using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Movement.Systems;

namespace Content.Server.Body.Systems;

public sealed partial class SlowdownOnBloodlossSystem : SharedSlowdownOnBloodlossSystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlowdownOnBloodlossComponent, SolutionChangedEvent>(SolutionChanged);
    }

    private void SolutionChanged(EntityUid uid, SlowdownOnBloodlossComponent comp, SolutionChangedEvent args)
    {
        if (!TryComp<BloodstreamComponent>(uid, out var bloodComp) || args.Solution != bloodComp.BloodSolution)
            return;

        foreach (var (threshold, multiplier) in comp.Thresholds)
        {
            if ((float) args.Solution.Volume < threshold)
                continue;

            comp.CurrentMultiplier = multiplier;
            Dirty(uid, comp);
            break;
        }

        _speedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }
}
