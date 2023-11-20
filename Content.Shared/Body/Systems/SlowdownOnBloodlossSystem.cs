using Content.Shared.Body.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Body.Systems;

public abstract partial class SharedSlowdownOnBloodlossSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlowdownOnBloodlossComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedRefresh);
    }

    private void MovementSpeedRefresh(EntityUid uid, SlowdownOnBloodlossComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(comp.CurrentMultiplier, comp.CurrentMultiplier);
    }
}
