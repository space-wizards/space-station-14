using Content.Shared.Movement.Systems;

namespace Content.Shared.Cuffs.Components;

public abstract class SharedLegCuffableSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedLegCuffableComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedModify);
        SubscribeLocalEvent<SharedLegCuffableComponent, LegcuffChangeEvent>(OnLegcuffChange);
    }

    private void OnLegcuffChange(EntityUid uid, SharedLegCuffableComponent component, LegcuffChangeEvent args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void MovementSpeedModify(EntityUid uid, SharedLegCuffableComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.IsCuffed)
            return;

        args.ModifySpeed(component.Slowdown, component.Slowdown);
    }
}
