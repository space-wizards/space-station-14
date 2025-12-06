using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Throwing;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    private void InitializeInteraction()
    {
        SubscribeLocalEvent<TriggerOnExaminedComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<TriggerOnActivateComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<TriggerOnUseComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<TriggerOnInteractHandComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<TriggerOnInteractUsingComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<TriggerOnThrowComponent, ThrowEvent>(OnThrow);
        SubscribeLocalEvent<TriggerOnThrownComponent, ThrownEvent>(OnThrown);

        SubscribeLocalEvent<TriggerOnUiOpenComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<TriggerOnUiCloseComponent, BoundUIClosedEvent>(OnUiClosed);

        SubscribeLocalEvent<ItemToggleOnTriggerComponent, TriggerEvent>(HandleItemToggleOnTrigger);
        SubscribeLocalEvent<AnchorOnTriggerComponent, TriggerEvent>(HandleAnchorOnTrigger);
        SubscribeLocalEvent<UseDelayOnTriggerComponent, TriggerEvent>(HandleUseDelayOnTrigger);
    }

    private void OnExamined(Entity<TriggerOnExaminedComponent> ent, ref ExaminedEvent args)
    {
        Trigger(ent.Owner, args.Examiner, ent.Comp.KeyOut);
    }

    private void OnActivate(Entity<TriggerOnActivateComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.RequireComplex && !args.Complex)
            return;

        Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
        args.Handled = true;
    }

    private void OnUse(Entity<TriggerOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
        args.Handled = true;
    }

    private void OnInteractHand(Entity<TriggerOnInteractHandComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
        args.Handled = true;
    }

    private void OnInteractUsing(Entity<TriggerOnInteractUsingComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_whitelist.CheckBoth(args.Used, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        Trigger(ent.Owner, ent.Comp.TargetUsed ? args.Used : args.User, ent.Comp.KeyOut);
        args.Handled = true;
    }

    private void OnThrow(Entity<TriggerOnThrowComponent> ent, ref ThrowEvent args)
    {
        Trigger(ent.Owner, args.Thrown, ent.Comp.KeyOut);
    }

    private void OnThrown(Entity<TriggerOnThrownComponent> ent, ref ThrownEvent args)
    {
        Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }

    private void OnUiOpened(Entity<TriggerOnUiOpenComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (ent.Comp.UiKeys == null || ent.Comp.UiKeys.Contains(args.UiKey))
        {
            Trigger(ent, args.Actor, ent.Comp.KeyOut);
        }
    }

    private void OnUiClosed(Entity<TriggerOnUiCloseComponent> ent, ref BoundUIClosedEvent args)
    {
        if (ent.Comp.UiKeys == null || ent.Comp.UiKeys.Contains(args.UiKey))
        {
            Trigger(ent, args.Actor, ent.Comp.KeyOut);
        }
    }

    private void HandleItemToggleOnTrigger(Entity<ItemToggleOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (!TryComp<ItemToggleComponent>(target, out var itemToggle))
            return;

        var handled = false;
        if (itemToggle.Activated && ent.Comp.CanDeactivate)
            handled = _itemToggle.TryDeactivate((target.Value, itemToggle), args.User, ent.Comp.Predicted, ent.Comp.ShowPopup);
        else if (ent.Comp.CanActivate)
            handled = _itemToggle.TryActivate((target.Value, itemToggle), args.User, ent.Comp.Predicted, ent.Comp.ShowPopup);

        args.Handled |= handled;
    }

    private void HandleAnchorOnTrigger(Entity<AnchorOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        var xform = Transform(target.Value);

        if (xform.Anchored && ent.Comp.CanUnanchor)
            _transform.Unanchor(target.Value, xform);
        else if (ent.Comp.CanAnchor)
            _transform.AnchorEntity(target.Value, xform);

        if (ent.Comp.RemoveOnTrigger)
            RemCompDeferred<AnchorOnTriggerComponent>(target.Value);

        args.Handled = true;
    }

    private void HandleUseDelayOnTrigger(Entity<UseDelayOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        args.Handled |= _useDelay.TryResetDelay(target.Value, ent.Comp.CheckDelayed);
    }
}
