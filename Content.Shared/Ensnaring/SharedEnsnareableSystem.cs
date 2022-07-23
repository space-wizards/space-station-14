using Content.Shared.Ensnaring.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Ensnaring;

public abstract class SharedEnsnareableSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedEnsnareableComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedModify);
        SubscribeLocalEvent<SharedEnsnareableComponent, EnsnareChangeEvent>(OnEnsnareChange);
    }

    private void OnEnsnareChange(EntityUid uid, SharedEnsnareableComponent component, EnsnareChangeEvent args)
    {
        component.WalkSpeed = args.WalkSpeed;
        component.SprintSpeed = args.SprintSpeed;

        _speedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void MovementSpeedModify(EntityUid uid, SharedEnsnareableComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        //This works perfectly with the event.
        if (!component.IsEnsnared)
            return;

        args.ModifySpeed(component.WalkSpeed, component.SprintSpeed);
    }
}
