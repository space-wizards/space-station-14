using Content.Server.Pulling;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Hands
{
    [UsedImplicitly]
    public sealed class HandVirtualPullSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandVirtualPullComponent, DroppedEvent>(HandlePullerDropped);
            SubscribeLocalEvent<HandVirtualPullComponent, UnequippedHandEvent>(HandlePullerUnequipped);

            SubscribeLocalEvent<HandVirtualPullComponent, BeforeInteractEvent>(HandleBeforeInteract);
        }

        private static void HandleBeforeInteract(
            EntityUid uid,
            HandVirtualPullComponent component,
            BeforeInteractEvent args)
        {
            // No interactions with a virtual pull, please.
            args.Handled = true;
        }

        // If the virtual pull gets removed from the hands for any reason, cancel the pull and delete it.
        private void HandlePullerUnequipped(EntityUid uid, HandVirtualPullComponent component, UnequippedHandEvent args)
        {
            MaybeDelete(component, args.User);
        }

        private void HandlePullerDropped(EntityUid uid, HandVirtualPullComponent component, DroppedEvent args)
        {
            MaybeDelete(component, args.User);
        }

        private void MaybeDelete(HandVirtualPullComponent comp, IEntity? user)
        {
            var pulled = comp.PulledEntity;

            if (!ComponentManager.TryGetComponent(pulled, out PullableComponent? pullable))
                return;

            if (pullable.Puller != user)
                return;

            pullable.TryStopPull(user);
            comp.Owner.QueueDelete();
        }
    }
}
