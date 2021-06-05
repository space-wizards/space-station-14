#nullable enable
using Content.Server.GameObjects.Components.Buckle;
using Content.Server.GameObjects.Components.Strap;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class BuckleSystem : SharedBuckleSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(InteractionSystem));
            UpdatesAfter.Add(typeof(InputSystem));

            SubscribeLocalEvent<BuckleComponent, MoveEvent>(MoveEvent);

            SubscribeLocalEvent<StrapComponent, RotateEvent>(RotateEvent);

            SubscribeLocalEvent<BuckleComponent, EntInsertedIntoContainerMessage>(ContainerModifiedBuckle);
            SubscribeLocalEvent<StrapComponent, EntInsertedIntoContainerMessage>(ContainerModifiedStrap);

            SubscribeLocalEvent<BuckleComponent, EntRemovedFromContainerMessage>(ContainerModifiedBuckle);
            SubscribeLocalEvent<StrapComponent, EntRemovedFromContainerMessage>(ContainerModifiedStrap);

            SubscribeLocalEvent<BuckleComponent, InteractHandEvent>(HandleAttackHand);
        }

        private void HandleAttackHand(EntityUid uid, BuckleComponent component, InteractHandEvent args)
        {
            args.Handled = component.TryUnbuckle(args.User);
        }

        public override void Update(float frameTime)
        {
            foreach (var (buckle, physics) in ComponentManager.EntityQuery<BuckleComponent, PhysicsComponent>())
            {
                buckle.Update(physics);
            }
        }

        private void MoveEvent(EntityUid uid, BuckleComponent buckle, MoveEvent ev)
        {
            var strap = buckle.BuckledTo;

            if (strap == null)
            {
                return;
            }

            var strapPosition = strap.Owner.Transform.Coordinates.Offset(buckle.BuckleOffset);

            if (ev.NewPosition.InRange(EntityManager, strapPosition, 0.2f))
            {
                return;
            }

            buckle.TryUnbuckle(buckle.Owner, true);
        }

        private void RotateEvent(EntityUid uid, StrapComponent strap, RotateEvent ev)
        {
            // On rotation of a strap, reattach all buckled entities.
            // This fixes buckle offsets and draw depths.
            foreach (var buckledEntity in strap.BuckledEntities)
            {
                if (!buckledEntity.TryGetComponent(out BuckleComponent? buckled))
                {
                    continue;
                }
                buckled.ReAttach(strap);
                buckled.Dirty();
            }
        }

        private void ContainerModifiedBuckle(EntityUid uid, BuckleComponent buckle, ContainerModifiedMessage message)
        {
            ContainerModifiedReAttach(buckle, buckle.BuckledTo);
        }
        private void ContainerModifiedStrap(EntityUid uid, StrapComponent strap, ContainerModifiedMessage message)
        {
            foreach (var buckledEntity in strap.BuckledEntities)
            {
                if (!buckledEntity.TryGetComponent(out BuckleComponent? buckled))
                {
                    continue;
                }

                ContainerModifiedReAttach(buckled, strap);
            }
        }

        private void ContainerModifiedReAttach(BuckleComponent buckle, StrapComponent? strap)
        {
            if (strap == null)
            {
                return;
            }

            var contained = buckle.Owner.TryGetContainer(out var ownContainer);
            var strapContained = strap.Owner.TryGetContainer(out var strapContainer);

            if (contained != strapContained || ownContainer != strapContainer)
            {
                buckle.TryUnbuckle(buckle.Owner, true);
                return;
            }

            if (!contained)
            {
                buckle.ReAttach(strap);
            }
        }
    }
}
