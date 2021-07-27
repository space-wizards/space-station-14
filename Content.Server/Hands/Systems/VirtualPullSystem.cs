using Content.Server.Pulling;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Hands
{
    [UsedImplicitly]
    public sealed class VirtualPullSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandVirtualPullComponent, DroppedEvent>(HandlePullerDropped);
        }

        private void HandlePullerDropped(EntityUid uid, HandVirtualPullComponent component, DroppedEvent args)
        {
            var pulled = component.PulledEntity;

            if (!ComponentManager.TryGetComponent(pulled, out PullableComponent? pullable))
                return;

            if (pullable.Puller != args.User)
                return;

            pullable.TryStopPull(args.User);
            component.Owner.QueueDelete();
        }
    }
}
