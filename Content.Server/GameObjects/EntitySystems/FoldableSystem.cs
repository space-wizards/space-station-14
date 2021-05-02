using System.Linq;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Strap;
using Content.Shared.Interfaces.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class FoldableSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FoldableComponent, AttackHandMessage>(OnPickup);
            SubscribeLocalEvent<FoldableComponent, UseInHandMessage>(OnUse);
            SubscribeLocalEvent<StrapComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        }
        public override void Shutdown()
        {
            base.Shutdown();
            UnsubscribeLocalEvent<FoldableComponent, AttackHandMessage>(OnPickup);
        }

        public static void ToggleFold(FoldableComponent component)
        {
            component.IsFolded = !component.IsFolded;
            if (component.Owner.TryGetComponent(out StrapComponent? strap))
                strap.Enabled = !component.IsFolded;
        }

        public static void SetFolded(FoldableComponent component, bool folded)
        {
            component.IsFolded = folded;
            if (component.Owner.TryGetComponent(out StrapComponent? strap))
                strap.Enabled = !component.IsFolded;
        }


        private void OnUse(EntityUid uid, FoldableComponent component, UseInHandMessage args)
        {
            // When used, drop the rollerbed and unfold it
            args.User.TryGetComponent(out HandsComponent? hands);
            hands?.Drop(args.Used);
            ToggleFold(component);
        }

        private void OnPickup(EntityUid uid, FoldableComponent component, AttackHandMessage args)
        {
            if (args.Handled) return;

            // First we check if the foldable object has a strap component
            if (component.Owner.TryGetComponent(out StrapComponent? strap))
            {
                // If an entity is buckled to the object we can't pick it up or fold it
                if (strap.BuckledEntities.Any())
                {
                    args.Handled = true;
                    return;
                }
            }

            // Then if there is no buckled entity we can fold it
            if (!component.IsFolded)
            {
                SetFolded(component, true);
                args.Handled = true;
                return;
            }

            // If it's already folded we don't interrupt the event
        }

        private void OnContainerModified(EntityUid uid, StrapComponent component, EntInsertedIntoContainerMessage args)
        {

        }

    }
}
