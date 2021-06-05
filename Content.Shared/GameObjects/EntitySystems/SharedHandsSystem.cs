using Content.Shared.GameObjects.Components.Items;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedHandsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedHandsComponent, DropHandItemsEvent>(HandleDown);
        }

        private void HandleDown(EntityUid uid, SharedHandsComponent component, DropHandItemsEvent args)
        {
            var msg = new BlockDropHandItemsEvent();
            EntityManager.EventBus.RaiseLocalEvent(uid, msg);

            if (msg.Cancelled) return;

            DropAllItemsInHands(EntityManager.GetEntity(uid), false);
        }

        public virtual void DropAllItemsInHands(IEntity entity, bool doMobChecks = true)
        {
        }
    }

    /// <summary>
    /// Cancel if you don't want to drop items on an entity being downed.
    /// </summary>
    public sealed class DropHandItemsEvent : EntityEventArgs
    {
    }

    /// <summary>
    /// Cancel whether an entity should drop all of its items.
    /// </summary>
    public sealed class BlockDropHandItemsEvent : CancellableEntityEventArgs {}
}
