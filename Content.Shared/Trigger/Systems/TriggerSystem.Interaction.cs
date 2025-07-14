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
        var xform = Transform(ent);

        if (xform.Anchored)
            _transform.Unanchor(ent, xform);
        else
            _transform.AnchorEntity(ent, xform);

        if (ent.Comp.RemoveOnTrigger)
            RemCompDeferred<AnchorOnTriggerComponent>(ent);
    }
}
