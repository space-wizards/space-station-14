using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Strap;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;

// ReSharper disable MemberCanBePrivate.Global

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class FoldableSystem : EntitySystem
    {
        private InteractionSystem _interactionSystem => EntitySystemManager.GetEntitySystem<InteractionSystem>();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FoldableComponent, AttackHandMessage>(OnPickup);
            SubscribeLocalEvent<FoldableComponent, UseInHandMessage>(OnUse);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<FoldableComponent, AttackHandMessage>();
            UnsubscribeLocalEvent<FoldableComponent, UseInHandMessage>();
        }


        #region Helpers

        /// <summary>
        /// Toggle the provided <see cref="FoldableComponent"/> between folded/unfolded states
        /// </summary>
        public static void ToggleFold(FoldableComponent component)
        {
            SetFolded(component, !component.IsFolded);
        }


        /// <summary>
        /// Force the given <see cref="FoldableComponent"/> to be either folded or unfolded
        /// </summary>
        /// <param name="component"></param>
        /// <param name="folded">If true, the component will become folded, else unfolded</param>
        public static void SetFolded(FoldableComponent component, bool folded)
        {
            component.IsFolded = folded;

            if (component.Owner.TryGetComponent(out StrapComponent? strap))
                strap.Enabled = !component.IsFolded;

            if (component.Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
                physicsComponent.CanCollide = !component.IsFolded;
        }

        private static void Deploy(FoldableComponent component, IEntity user, MapCoordinates newPos)
        {
            // When used, drop the foldable and unfold it
            if (user.TryGetComponent(out HandsComponent? hands));
                hands?.Drop(component.Owner);

            // Deploy the foldable in the looking direction
            component.Owner.Transform.LocalPosition = newPos.Position;
            SetFolded(component, false);
        }

        #endregion



        #region Event handlers

        // When clicked in hand, unfold in front of the user
        private void OnUse(EntityUid uid, FoldableComponent component, UseInHandMessage args)
        {
            var userTransform = args.User.Transform;
            var offsetCoords =
                new MapCoordinates(userTransform.MapPosition.Position + userTransform.LocalRotation.ToWorldVec(), userTransform.MapID);

            // Check nothing blocks the way
            if (_interactionSystem.UnobstructedDistance(
                userTransform.MapPosition, offsetCoords, predicate: (IEntity entity) => entity.Equals(args.User))
                >= 1.5f)
            {
                var message = Loc.GetString("comp-foldable-deploy-fail", ("object", component.Owner.Name));
                args.User.PopupMessage(message);
                return;
            }

            Deploy(component, args.User, offsetCoords);
        }


        // Handle pickup events and block them if the object is unfolded
        private void OnPickup(EntityUid uid, FoldableComponent component, AttackHandMessage args)
        {
            if (args.Handled) return;

            // First we check if the foldable object has a strap component
            if (component.Owner.TryGetComponent(out StrapComponent? strap))
            {
                // If an entity is buckled to the object we can't pick it up or fold it
                if (strap.BuckledEntities.Any())
                    return;
            }

            // Then if there is no buckled entity we can fold it
            if (!component.IsFolded)
            {
                SetFolded(component, true);
                args.Handled = true;
                return;
            }

            // Else, pick it up
        }

        #endregion

    }
}
