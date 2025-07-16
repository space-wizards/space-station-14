using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem : EntitySystem
{
    private void InitializeInteraction()
    {
        SubscribeLocalEvent<TriggerOnActivateComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<TriggerOnUseComponent, UseInHandEvent>(OnUse);

        SubscribeLocalEvent<AnchorOnTriggerComponent, TriggerEvent>(HandleAnchorOnTrigger);
        SubscribeLocalEvent<UseDelayOnTriggerComponent, TriggerEvent>(HandleUseDelayOnTrigger);
    }

    private void OnActivate(Entity<TriggerOnActivateComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        Trigger(ent.Owner, args.User, ent.Comp.TriggerKey);
        args.Handled = true;
    }

    private void OnUse(Entity<TriggerOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Trigger(ent.Owner, args.User, ent.Comp.TriggerKey);
        args.Handled = true;
    }

    private void HandleAnchorOnTrigger(Entity<AnchorOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.EffectKeys.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        var xform = Transform(target.Value);

        if (xform.Anchored)
            _transform.Unanchor(target.Value, xform);
        else
            _transform.AnchorEntity(target.Value, xform);

        if (ent.Comp.RemoveOnTrigger)
            RemCompDeferred<AnchorOnTriggerComponent>(target.Value);

        args.Handled = true;
    }

    private void HandleUseDelayOnTrigger(Entity<UseDelayOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.EffectKeys.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        args.Handled |= _useDelay.TryResetDelay(target.Value, ent.Comp.CheckDelayed);
    }
}
