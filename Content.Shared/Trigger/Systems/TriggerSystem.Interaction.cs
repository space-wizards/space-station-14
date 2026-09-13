using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Throwing;
using Content.Shared.Timing.Systems;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{

    [SubscribeLocalEvent]
    private void OnExamined(Entity<TriggerOnExaminedComponent> ent, ref ExaminedEvent args)
    {
        Trigger(ent.Owner, args.Examiner, ent.Comp.KeyOut);
    }

    [SubscribeLocalEvent]
    private void OnActivate(Entity<TriggerOnActivateComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.RequireComplex && !args.Complex)
            return;

        Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnUse(Entity<TriggerOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnInteractHand(Entity<TriggerOnInteractHandComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnUserInteractHand(Entity<TriggerOnUserInteractHandComponent> ent, ref UserInteractHandEvent args)
    {
        if (args.Handled)
            return;

        Trigger(ent.Owner, args.Target, ent.Comp.KeyOut);

        if (ent.Comp.Handle)
            args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<TriggerOnInteractUsingComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_whitelist.CheckBoth(args.Used, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        Trigger(ent.Owner, ent.Comp.TargetUsed ? args.Used : args.User, ent.Comp.KeyOut);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnUserInteractUsing(Entity<TriggerOnUserInteractUsingComponent> ent, ref UserInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_whitelist.CheckBoth(args.Used, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        Trigger(ent.Owner, ent.Comp.TargetUsed ? args.Used : args.Target, ent.Comp.KeyOut);

        if (ent.Comp.Handle)
            args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnThrow(Entity<TriggerOnThrowComponent> ent, ref ThrowEvent args)
    {
        Trigger(ent.Owner, args.Thrown, ent.Comp.KeyOut);
    }

    [SubscribeLocalEvent]
    private void OnThrown(Entity<TriggerOnThrownComponent> ent, ref ThrownEvent args)
    {
        Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }

    [SubscribeLocalEvent]
    private void OnUiOpened(Entity<TriggerOnUiOpenComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (ent.Comp.UiKeys == null || ent.Comp.UiKeys.Contains(args.UiKey))
        {
            Trigger(ent, args.Actor, ent.Comp.KeyOut);
        }
    }

    [SubscribeLocalEvent]
    private void OnUiClosed(Entity<TriggerOnUiCloseComponent> ent, ref BoundUIClosedEvent args)
    {
        if (ent.Comp.UiKeys == null || ent.Comp.UiKeys.Contains(args.UiKey))
        {
            Trigger(ent, args.Actor, ent.Comp.KeyOut);
        }
    }
}

public sealed partial class ItemToggleOnTriggerSystem : XOnTriggerSystem<ItemToggleOnTriggerComponent>
{
    [Dependency] private ItemToggleSystem _itemToggle = default!;

    protected override void OnTrigger(Entity<ItemToggleOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!TryComp<ItemToggleComponent>(target, out var itemToggle))
            return;

        var handled = false;
        if (itemToggle.Activated && ent.Comp.CanDeactivate)
            handled = _itemToggle.TryDeactivate((target, itemToggle), args.User, ent.Comp.Predicted, ent.Comp.ShowPopup, ent.Comp.ConsciousAction);
        else if (ent.Comp.CanActivate)
            handled = _itemToggle.TryActivate((target, itemToggle), args.User, ent.Comp.Predicted, ent.Comp.ShowPopup, ent.Comp.ConsciousAction);

        args.Handled |= handled;
    }
}

public sealed partial class AnchorOnTriggerSystem : XOnTriggerSystem<AnchorOnTriggerComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void OnTrigger(Entity<AnchorOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var xform = Transform(target);

        if (xform.Anchored && ent.Comp.CanUnanchor)
            _transform.Unanchor(target, xform);
        else if (ent.Comp.CanAnchor)
            _transform.AnchorEntity(target, xform);

        if (ent.Comp.RemoveOnTrigger)
            RemCompDeferred<AnchorOnTriggerComponent>(target);

        args.Handled = true;
    }
}

public sealed partial class UseDelayOnTriggerSystem : XOnTriggerSystem<UseDelayOnTriggerComponent>
{
    [Dependency] private UseDelaySystem _useDelay = default!;

    protected override void OnTrigger(Entity<UseDelayOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        args.Handled |= _useDelay.TryResetDelay(target, ent.Comp.CheckDelayed);
    }
}
