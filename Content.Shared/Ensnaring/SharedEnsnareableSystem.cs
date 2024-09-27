using Content.Shared.DoAfter;
using Content.Shared.Ensnaring.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
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
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

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

    /// <summary>
    /// Used where you want to try to free an entity with the <see cref="EnsnareableComponent"/>
    /// </summary>
    /// <param name="target">The entity that will be freed</param>
    /// <param name="user">The entity that is freeing the target</param>
    /// <param name="ensnare">The entity used to ensnare</param>
    /// <param name="component">The ensnaring component</param>
    public void TryFree(EntityUid target, EntityUid user, EntityUid ensnare, EnsnaringComponent component)
    {
        // Don't do anything if they don't have the ensnareable component.
        if (!HasComp<EnsnareableComponent>(target))
            return;

        var freeTime = user == target ? component.BreakoutTime : component.FreeTime;
        var breakOnMove = !component.CanMoveBreakout;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, freeTime, new EnsnareableDoAfterEvent(), target, target: target, used: ensnare)
        {
            BreakOnMove = breakOnMove,
            BreakOnDamage = false,
            NeedHand = true,
            BreakOnDropItem = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
            return;

        if (user == target)
            Popup.PopupPredicted(Loc.GetString("ensnare-component-try-free", ("ensnare", ensnare)), target, target);
        else
            Popup.PopupPredicted(Loc.GetString("ensnare-component-try-free-other", ("ensnare", ensnare), ("user", Identity.Entity(target, EntityManager))), user, user);
    }
}
