using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using System.Collections.Generic;

namespace Content.Shared.Hands
{
    public abstract class SharedHandsSystem : EntitySystem
    {
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeAllEvent<RequestSetHandEvent>(HandleSetHand);

            SubscribeLocalEvent<SharedHandsComponent, EntRemovedFromContainerMessage>(HandleContainerModified);
            SubscribeLocalEvent<SharedHandsComponent, EntInsertedIntoContainerMessage>(HandleContainerModified);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.Drop, new PointerInputCmdHandler(DropPressed))
                .Bind(ContentKeyFunctions.SwapHands, InputCmdHandler.FromDelegate(SwapHandsPressed, handle: false))
                .Register<SharedHandsSystem>();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            CommandBinds.Unregister<SharedHandsSystem>();
        }

        #region interactions
        private void SwapHandsPressed(ICommonSession? session)
        {
            var player = session?.AttachedEntityUid;
            if (player == null)
                return;

            if (!EntityManager.TryGetComponent(player.Value, out SharedHandsComponent hands))
                return;

            if (!hands.TryGetSwapHandsResult(out var nextHand))
                return;

            TrySetActiveHand(session!.AttachedEntityUid!.Value, nextHand, hands);
        }

        private bool DropPressed(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            var player = session?.AttachedEntity;

            if (player == null)
                return false;

            if (!player.TryGetComponent(out SharedHandsComponent? hands))
                return false;

            var activeHand = hands.ActiveHand;

            if (activeHand == null)
                return false;

            hands.TryDropHand(activeHand, coords);
            return false;
        }
        #endregion

        #region EntityInsertRemove
        /// <summary>
        ///     Removes the contents of a hand from its container. Assumes that the removal is allowed.
        /// </summary>
        public virtual void RemoveHeldEntityFromHand(EntityUid uid, Hand hand, SharedHandsComponent? hands = null)
        {
            if (!Resolve(uid, ref hands))
                return;

            var entity = hand.Container?.ContainedEntity;

            if (entity == null)
                return;

            var owner = EntityManager.GetEntity(uid);

            if (!hand.Container!.Remove(entity))
            {
                Logger.Error($"{nameof(SharedHandsComponent)} on {owner} could not remove {entity} from {hand.Container}.");
                return;
            }

            _interactionSystem.UnequippedHandInteraction(owner, entity, hand);

            if (hand.Name == hands.ActiveHand)
                RaiseLocalEvent(entity.Uid, new HandDeselectedEvent(uid, entity.Uid), false);

            if (EntityManager.TryGetComponent(entity.Uid, out SharedItemComponent? item))
                item.RemovedFromSlot();

            hands.Dirty();
        }

        
        /// <summary>
        ///     Puts an entity into the player's hand, assumes that the insertion is allowed.
        /// </summary>
        public virtual void PutEntityIntoHand(EntityUid uid, Hand hand, IEntity entity, SharedHandsComponent? hands = null)
        {
            if (!Resolve(uid, ref hands))
                return;

            var handContainer = hand.Container;
            if (handContainer == null || handContainer.ContainedEntity != null)
                return;

            var owner = EntityManager.GetEntity(uid);
            if (!handContainer.Insert(entity))
            {
                Logger.Error($"{nameof(SharedHandsComponent)} on {owner} could not insert {entity} into {handContainer}.");
                return;
            }

            _interactionSystem.EquippedHandInteraction(owner, entity, hand);

            if (hand.Name == hands.ActiveHand)
                RaiseLocalEvent(entity.Uid, new HandSelectedEvent(uid, entity.Uid), false);

            if (EntityManager.TryGetComponent(entity.Uid, out SharedItemComponent? item))
                item.EquippedToSlot();

            entity.Transform.LocalPosition = Vector2.Zero;

            hands.Dirty();
        }

        public abstract void PickupAnimation(EntityUid uid, IEntity item, bool animateUser,
            EntityCoordinates initialPosition, Vector2 finalPosition);
        #endregion

        #region visuals
        protected virtual void HandleContainerModified(EntityUid uid, SharedHandsComponent hands, ContainerModifiedMessage args)
        {
            UpdateHandVisualizer(uid, hands);
        }

        /// <summary>
        ///     Update the In-Hand sprites
        /// </summary>
        public void UpdateHandVisualizer(EntityUid uid, SharedHandsComponent? handComp = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref handComp, ref appearance))
                return;

            var handsVisuals = new List<HandVisualState>();
            foreach (var hand in handComp.Hands)
            {
                if (hand.HeldEntity == null)
                    continue;

                if (!EntityManager.TryGetComponent(hand.HeldEntity.Uid, out SharedItemComponent? item) || item.RsiPath == null)
                    continue;

                var handState = new HandVisualState(item.RsiPath, item.EquippedPrefix, hand.Location, item.Color);
                handsVisuals.Add(handState);
            }

            appearance.SetData(HandsVisuals.VisualState, new HandsVisualState(handsVisuals));
        }
        #endregion

        private void HandleSetHand(RequestSetHandEvent msg, EntitySessionEventArgs eventArgs)
        {
            if (eventArgs.SenderSession.AttachedEntityUid == null)
                return;

            TrySetActiveHand(eventArgs.SenderSession.AttachedEntityUid.Value, msg.HandName);
        }

        /// <summary>
        ///     Set the currently active hand and raise hand (de)selection events directed at the held entities.
        /// </summary>
        /// <returns>True if the active hand was set to a NEW value. Setting it to the same value returns false and does
        /// not trigger interactions.</returns>
        public virtual bool TrySetActiveHand(EntityUid uid, string? value, SharedHandsComponent? handComp = null)
        {
            if (!Resolve(uid, ref handComp))
                return false;

            if (value == handComp.ActiveHand)
                return false;

            if (value != null && !handComp.HasHand(value))
            {
                Logger.Warning($"{nameof(SharedHandsComponent)} on {handComp.Owner} tried to set its active hand to {value}, which was not a hand.");
                return false;
            }
            if (value == null && handComp.Hands.Count != 0)
            {
                Logger.Error($"{nameof(SharedHandsComponent)} on {handComp.Owner} tried to set its active hand to null, when it still had another hand.");
                return false;
            }

            if (handComp.TryGetActiveHeldEntity(out var entity))
                RaiseLocalEvent(entity.Uid, new HandDeselectedEvent(uid, entity.Uid), false);

            handComp.ActiveHand = value;

            if (handComp.TryGetActiveHeldEntity(out entity))
                RaiseLocalEvent(entity.Uid, new HandSelectedEvent(uid, entity.Uid), false);

            handComp.Dirty();
            return true;
        }
    }
}
