using Content.Shared.DoAfter;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Ensnaring;

[Serializable, NetSerializable]
public sealed partial class EnsnareableDoAfterEvent : SimpleDoAfterEvent
{
}

public abstract class SharedEnsnareableSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsnareableComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedModify);
        SubscribeLocalEvent<EnsnareableComponent, EnsnareEvent>(OnEnsnare);
        SubscribeLocalEvent<EnsnareableComponent, EnsnareRemoveEvent>(OnEnsnareRemove);
        SubscribeLocalEvent<EnsnareableComponent, EnsnaredChangedEvent>(OnEnsnareChange);
        SubscribeLocalEvent<EnsnareableComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<EnsnareableComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, EnsnareableComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not EnsnareableComponentState state)
            return;

        if (state.IsEnsnared == component.IsEnsnared)
            return;

        component.IsEnsnared = state.IsEnsnared;
        RaiseLocalEvent(uid, new EnsnaredChangedEvent(component.IsEnsnared));
    }

    private void OnGetState(EntityUid uid, EnsnareableComponent component, ref ComponentGetState args)
    {
        args.State = new EnsnareableComponentState(component.IsEnsnared);
    }

    private void OnEnsnare(EntityUid uid, EnsnareableComponent component, EnsnareEvent args)
    {
        component.WalkSpeed *= args.WalkSpeed;
        component.SprintSpeed *= args.SprintSpeed;

        _speedModifier.RefreshMovementSpeedModifiers(uid);

        var ev = new EnsnaredChangedEvent(component.IsEnsnared);
        RaiseLocalEvent(uid, ev);
    }

    private void OnEnsnareRemove(EntityUid uid, EnsnareableComponent component, EnsnareRemoveEvent args)
    {
        component.WalkSpeed /= args.WalkSpeed;
        component.SprintSpeed /= args.SprintSpeed;

        _speedModifier.RefreshMovementSpeedModifiers(uid);

        var ev = new EnsnaredChangedEvent(component.IsEnsnared);
        RaiseLocalEvent(uid, ev);
    }

    private void OnEnsnareChange(EntityUid uid, EnsnareableComponent component, EnsnaredChangedEvent args)
    {
        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, EnsnareableComponent component, AppearanceComponent? appearance = null)
    {
        Appearance.SetData(uid, EnsnareableVisuals.IsEnsnared, component.IsEnsnared, appearance);
    }

    private void MovementSpeedModify(EntityUid uid, EnsnareableComponent component,
        RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.IsEnsnared)
            return;

        args.ModifySpeed(component.WalkSpeed, component.SprintSpeed);
    }
}
