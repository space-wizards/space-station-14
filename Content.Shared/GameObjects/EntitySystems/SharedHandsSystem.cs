using Content.Shared.GameObjects.Components.Items;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedHandsSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleContainerModified);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerModified);

            SubscribeLocalEvent<SwapHandsMessage>(HandleSwapHands);
            SubscribeNetworkEvent<SwapHandsMessage>(HandleSwapHands);
        }

        private void HandleSwapHands(SwapHandsMessage msg, EntitySessionEventArgs eventArgs)
        {
            var entity = eventArgs.SenderSession?.AttachedEntity;

            if (entity == null || !entity.TryGetComponent(out SharedHandsComponent? hands))
                return;

            hands.SwapHands();
        }

        protected abstract void HandleContainerModified(ContainerModifiedMessage args);
    }

    [Serializable, NetSerializable]
    public class SwapHandsMessage : EntityEventArgs
    {
    }
}
