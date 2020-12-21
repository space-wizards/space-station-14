#nullable enable
using Content.Server.GameObjects.Components.Buckle;
using Content.Server.GameObjects.Components.Strap;
using Content.Server.GameObjects.EntitySystems.Click;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class BuckleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(InteractionSystem));
            UpdatesAfter.Add(typeof(InputSystem));

            SubscribeLocalEvent<MoveEvent>(MoveEvent);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(ContainerModified);
            SubscribeLocalEvent<EntRemovedFromContainerMessage>(ContainerModified);
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<BuckleComponent>(false))
            {
                comp.Update();
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<MoveEvent>();
        }

        private void MoveEvent(MoveEvent ev)
        {
            if (!ev.Sender.TryGetComponent(out BuckleComponent? buckle))
            {
                return;
            }

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

        private void ContainerModified(ContainerModifiedMessage message)
        {
            // Not returning is necessary in case an entity has both a buckle and strap component
            if (message.Entity.TryGetComponent(out BuckleComponent? buckle))
            {
                ContainerModifiedReAttach(buckle, buckle.BuckledTo);
            }

            if (message.Entity.TryGetComponent(out StrapComponent? strap))
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
