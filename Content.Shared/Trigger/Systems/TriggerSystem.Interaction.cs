using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    private void InitializeInteraction()
    {
        SubscribeLocalEvent<TriggerOnActivateComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<TriggerOnUseComponent, UseInHandEvent>(OnUse);

        SubscribeLocalEvent<ItemToggleOnTriggerComponent, TriggerEvent>(HandleItemToggleOnTrigger);
        SubscribeLocalEvent<AnchorOnTriggerComponent, TriggerEvent>(HandleAnchorOnTrigger);
        SubscribeLocalEvent<UseDelayOnTriggerComponent, TriggerEvent>(HandleUseDelayOnTrigger);
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
