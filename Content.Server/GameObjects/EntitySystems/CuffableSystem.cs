#nullable enable
using Content.Server.GameObjects.Components.ActionBlocking;
using Content.Server.GameObjects.Components.GUI;
using Content.Shared.GameObjects.Components.Items;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class CuffableSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityManager.EventBus.SubscribeEvent<HandCountChangedEvent>(EventSource.Local, this, OnHandCountChanged);
        }

        /// <summary>
        ///     Check the current amount of hands the owner has, and if there's less hands than active cuffs we remove some cuffs.
        /// </summary>
        private void OnHandCountChanged(HandCountChangedEvent message)
        {
            var owner = message.Sender;

            if (!owner.TryGetComponent(out CuffableComponent? cuffable) ||
                !cuffable.Initialized) return;

            var dirty = false;
            var handCount = owner.GetComponentOrNull<HandsComponent>()?.Count ?? 0;

            while (cuffable.CuffedHandCount > handCount && cuffable.CuffedHandCount > 0)
            {
                dirty = true;

                var container = cuffable.Container;
                var entity = container.ContainedEntities[^1];

                container.Remove(entity);
                entity.Transform.WorldPosition = owner.Transform.WorldPosition;
            }

            if (dirty)
            {
                cuffable.CanStillInteract = handCount > cuffable.CuffedHandCount;
                cuffable.CuffedStateChanged();
                cuffable.Dirty();
            }
        }
    }
}
