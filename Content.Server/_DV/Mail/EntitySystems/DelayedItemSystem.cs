using Content.Server._DV.Mail.Components;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Robust.Shared.Containers;

namespace Content.Server._DV.Mail.EntitySystems;

/// <summary>
/// A placeholder for another entity, spawned when taken out of a container, with the placeholder deleted shortly after.
/// Useful for storing instant effect entities, e.g. smoke, in the mail.
/// </summary>
public sealed class DelayedItemSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DelayedItemComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<DelayedItemComponent, GotEquippedHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<DelayedItemComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<DelayedItemComponent, ContainerModifiedMessage>(OnRemovedFromContainer);
    }

    /// <summary>
    /// EntGotRemovedFromContainerMessage handler - spawn the intended entity after removed from a container.
    /// </summary>
    private void OnRemovedFromContainer(Entity<DelayedItemComponent> ent, ref ContainerModifiedMessage args)
    {
        Spawn(ent.Comp.Item, Transform(ent).Coordinates);
    }

    /// <summary>
    /// GotEquippedHandEvent handler - destroy the placeholder.
    /// </summary>
    private void OnHandEquipped(Entity<DelayedItemComponent> ent, ref GotEquippedHandEvent args)
    {
        EntityManager.QueueDeleteEntity(ent);
    }

    /// <summary>
    /// OnDropAttempt handler - destroy the placeholder.
    /// </summary>
    private void OnDropAttempt(Entity<DelayedItemComponent> ent, ref DropAttemptEvent args)
    {
        EntityManager.DeleteEntity(ent);
    }

    /// <summary>
    /// OnDamageChanged handler - item has taken damage (e.g. inside the envelope), spawn the intended entity outside of any container and delete the placeholder.
    /// </summary>
    private void OnDamageChanged(Entity<DelayedItemComponent> ent, ref DamageChangedEvent args)
    {
        Spawn(ent.Comp.Item, Transform(ent).Coordinates);
        EntityManager.DeleteEntity(ent);
    }
}
