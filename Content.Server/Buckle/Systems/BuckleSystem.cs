using Content.Server.Buckle.Components;
using Content.Server.Interaction;
using Content.Shared.Buckle;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Buckle.Systems
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

            SubscribeLocalEvent<BuckleComponent, InteractHandEvent>(HandleInteractHand);

            SubscribeLocalEvent<BuckleComponent, GetInteractionVerbsEvent>(AddUnbuckleVerb);
        }

        private void AddUnbuckleVerb(EntityUid uid, BuckleComponent component, GetInteractionVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract || !component.Buckled)
                return;

            Verb verb = new()
            {
                Act = () => component.TryUnbuckle(args.User),
                Text = Loc.GetString("verb-categories-unbuckle"),
                IconTexture = "/Textures/Interface/VerbIcons/unbuckle.svg.192dpi.png"
            };

            if (args.Target == args.User && args.Using == null)
            {
                // A user is left clicking themselves with an empty hand, while buckled.
                // It is very likely they are trying to unbuckle themselves.
                verb.Priority = 1;
            }

            args.Verbs.Add(verb);
        }

        private void HandleInteractHand(EntityUid uid, BuckleComponent component, InteractHandEvent args)
        {
            args.Handled = component.TryUnbuckle(args.User);
        }

        private void MoveEvent(EntityUid uid, BuckleComponent buckle, ref MoveEvent ev)
        {
            var strap = buckle.BuckledTo;

            if (strap == null)
            {
                return;
            }

            var strapPosition = EntityManager.GetComponent<TransformComponent>(strap.Owner).Coordinates.Offset(buckle.BuckleOffset);

            if (ev.NewPosition.InRange(EntityManager, strapPosition, strap.MaxBuckleDistance))
            {
                return;
            }

            buckle.TryUnbuckle(buckle.Owner, true);
        }

        private void RotateEvent(EntityUid uid, StrapComponent strap, ref RotateEvent ev)
        {
            // On rotation of a strap, reattach all buckled entities.
            // This fixes buckle offsets and draw depths.
            foreach (var buckledEntity in strap.BuckledEntities)
            {
                if (!EntityManager.TryGetComponent(buckledEntity, out BuckleComponent? buckled))
                {
                    continue;
                }
                buckled.ReAttach(strap);
                Dirty(buckled);
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
                if (!EntityManager.TryGetComponent(buckledEntity, out BuckleComponent? buckled))
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
