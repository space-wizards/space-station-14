using Content.Shared.GameObjects.Components.Items;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedHandsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedHandsComponent, DownEvent>(HandleDown);
        }

        private void HandleDown(EntityUid uid, SharedHandsComponent component, DownEvent args)
        {
            var msg = new DropItemsOnDownEvent();

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
    public sealed class DropItemsOnDownEvent : CancellableEntityEventArgs
    {

    }
}
