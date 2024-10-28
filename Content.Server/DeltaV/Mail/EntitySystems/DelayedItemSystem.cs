using Content.Server.DeltaV.Mail.Components;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Robust.Shared.Containers;

namespace Content.Server.DeltaV.Mail.EntitySystems
{
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
            SubscribeLocalEvent<DelayedItemComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
        }

        /// <summary>
        /// EntGotRemovedFromContainerMessage handler - spawn the intended entity after removed from a container.
        /// </summary>
        private void OnRemovedFromContainer(EntityUid uid, DelayedItemComponent component, ContainerModifiedMessage args)
        {
            Spawn(component.Item, Transform(uid).Coordinates);
        }

        /// <summary>
        /// GotEquippedHandEvent handler - destroy the placeholder.
        /// </summary>
        private void OnHandEquipped(EntityUid uid, DelayedItemComponent component, EquippedHandEvent args)
        {
            EntityManager.DeleteEntity(uid);
        }

        /// <summary>
        /// OnDropAttempt handler - destroy the placeholder.
        /// </summary>
        private void OnDropAttempt(EntityUid uid, DelayedItemComponent component, DropAttemptEvent args)
        {
            EntityManager.DeleteEntity(uid);
        }

        /// <summary>
        /// OnDamageChanged handler - item has taken damage (e.g. inside the envelope), spawn the intended entity outside of any container and delete the placeholder.
        /// </summary>
        private void OnDamageChanged(EntityUid uid, DelayedItemComponent component, DamageChangedEvent args)
        {
            Spawn(component.Item, Transform(uid).Coordinates);
            EntityManager.DeleteEntity(uid);
        }
    }
}
