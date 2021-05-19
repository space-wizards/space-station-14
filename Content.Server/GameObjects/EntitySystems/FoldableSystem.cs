using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Strap;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.Components;
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
        public void ToggleFold(FoldableComponent component)
        {
            TrySetFolded(component, !component.IsFolded);
        }


        /// <summary>
        /// Force the given <see cref="FoldableComponent"/> to be either folded or unfolded.
        /// Enable/disable other components here.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="folded">If true, the component will become folded, else unfolded</param>
        private void SetFolded(FoldableComponent component, bool folded)
        {
            component.IsFolded = folded;

            // Disable buckling
            if (component.Owner.TryGetComponent(out StrapComponent? strap))
                strap.Enabled = !component.IsFolded;

            // Disable physics
            if (component.Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
                physicsComponent.CanCollide = !component.IsFolded;

            if (component.Owner.TryGetComponent(out EntityStorageComponent? storage))
            {
                storage.IsWeldedShut = component.IsFolded;
                if (component.IsFolded) storage.TryCloseStorage(component.Owner);
            }

            // Raise event
            var msg = new FoldedMessage(folded);
            RaiseLocalEvent(component.Owner.Uid, msg, false);
        }


        /// <summary>
        /// Try to set the folded state. Run checks on different components
        /// </summary>
        /// <param name="component"></param>
        /// <param name="folded"></param>
        /// <returns>Did it succeed</returns>
        public bool TrySetFolded(FoldableComponent component, bool folded)
        {
            // Prevent folding if an entity is buckled to this
            if (component.Owner.TryGetComponent(out StrapComponent? strap) &&
                strap.BuckledEntities.Any())
                return false;

            // If the container contains something, or it's open
            if (component.Owner.TryGetComponent(out EntityStorageComponent? storage) &&
                storage.Contents.ContainedEntities.Count > 0)
                return false;

            SetFolded(component, folded);
            return true;
        }


        private void Deploy(FoldableComponent component, IEntity user, MapCoordinates newPos)
        {
            // When used, drop the foldable and unfold it
            if (user.TryGetComponent(out HandsComponent? hands));
                hands?.Drop(component.Owner);

            // Deploy the foldable in the looking direction
            component.Owner.Transform.LocalPosition = newPos.Position;
            TrySetFolded(component, false);
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

            // Don't allow fold on click if it's a container
            if (component.Owner.TryGetComponent(out EntityStorageComponent? storage) &&
                !component.IsFolded)
                return;

            // Catch the pickup event and fold
            if (!component.IsFolded)
                args.Handled = !TrySetFolded(component, true);
        }

        #endregion
    }
}
