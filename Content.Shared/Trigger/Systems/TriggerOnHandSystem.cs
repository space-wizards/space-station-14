using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Timing;

namespace Content.Shared.Trigger.Systems;

public sealed partial class HandTriggerSystem : TriggerOnXSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnGotEquippedHandComponent, GotEquippedHandEvent>(OnGotEquipped);
        SubscribeLocalEvent<TriggerOnGotUnequippedHandComponent, GotUnequippedHandEvent>(OnGotUnequipped);
        SubscribeLocalEvent<TriggerOnDidEquipHandComponent, DidEquipHandEvent>(OnDidEquip);
        SubscribeLocalEvent<TriggerOnDidUnequipHandComponent, DidUnequipHandEvent>(OnDidUnequip);
        SubscribeLocalEvent<TriggerOnDroppedComponent, DroppedEvent>(OnDropped);
    }

    private void OnGotEquipped(Entity<TriggerOnGotEquippedHandComponent> ent, ref GotEquippedHandEvent args)
    {
        // If the entity was equipped on the server (without prediction) then the container change is networked to the client
        // which will raise the same event, but the effect of the trigger is already networked on its own. So this guard statement
        // prevents triggering twice on the client.
        if (_timing.ApplyingState)
            return;

        Trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }

    private void OnGotUnequipped(Entity<TriggerOnGotUnequippedHandComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        Trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }

    private void OnDidEquip(Entity<TriggerOnDidEquipHandComponent> ent, ref DidEquipHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        Trigger.Trigger(ent.Owner, args.Equipped, ent.Comp.KeyOut);
    }

    private void OnDidUnequip(Entity<TriggerOnDidUnequipHandComponent> ent, ref DidUnequipHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        Trigger.Trigger(ent.Owner, args.Unequipped, ent.Comp.KeyOut);
    }

    private void OnDropped(Entity<TriggerOnDroppedComponent> ent, ref DroppedEvent args)
    {
        // We don't need the guard statement here because this one is not a container event, but raised directly when interacting.
        Trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }
}
