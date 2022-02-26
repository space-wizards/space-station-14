using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
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
        [Dependency] private readonly SharedAdminLogSystem _adminLogSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeAllEvent<RequestSetHandEvent>(HandleSetHand);
            SubscribeLocalEvent<SharedHandsComponent, EntRemovedFromContainerMessage>(HandleContainerRemoved);
            SubscribeLocalEvent<SharedHandsComponent, EntInsertedIntoContainerMessage>(HandleContainerModified);
            SubscribeLocalEvent<SharedHandsComponent, ItemPrefixChangeEvent>(OnPrefixChanged);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.Drop, new PointerInputCmdHandler(DropPressed))
                .Bind(ContentKeyFunctions.SwapHands, InputCmdHandler.FromDelegate(SwapHandsPressed, handle: false))
                .Register<SharedHandsSystem>();
        }

        private void OnPrefixChanged(EntityUid uid, SharedHandsComponent component, ItemPrefixChangeEvent args)
        {
            // update hands visuals if this item is in a hand (rather then inventory or other container).
            if (component.HasHand(args.ContainerId))
            {
                UpdateHandVisuals(uid, component);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            CommandBinds.Unregister<SharedHandsSystem>();
        }

        #region interactions
        private void SwapHandsPressed(ICommonSession? session)
        {
            if (!TryComp(session?.AttachedEntity, out SharedHandsComponent? hands))
                return;

            if (!hands.TryGetSwapHandsResult(out var nextHand))
                return;

            TrySetActiveHand(hands.Owner, nextHand, hands);
        }

        private bool DropPressed(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (TryComp(session?.AttachedEntity, out SharedHandsComponent? hands))
                hands.TryDropActiveHand(coords);

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

            if (hand.Container?.ContainedEntity == null)
                return;

            var entity = hand.Container.ContainedEntity.Value;

            if (!hand.Container!.Remove(entity))
            {
                Logger.Error($"{nameof(SharedHandsComponent)} on {uid} could not remove {entity} from {hand.Container}.");
                return;
            }

            hands.Dirty();

            var gotUnequipped = new GotUnequippedHandEvent(uid, entity, hand);
            RaiseLocalEvent(entity, gotUnequipped, false);

            var didUnequip = new DidUnequipHandEvent(uid, entity, hand);
            RaiseLocalEvent(uid, didUnequip);

            if (hand.Name == hands.ActiveHand)
                RaiseLocalEvent(entity, new HandDeselectedEvent(uid, entity), false);
        }
        
        /// <summary>
        ///     Puts an entity into the player's hand, assumes that the insertion is allowed.
        /// </summary>
        public virtual void PutEntityIntoHand(EntityUid uid, Hand hand, EntityUid entity, SharedHandsComponent? hands = null)
        {
            if (!Resolve(uid, ref hands))
                return;

            var handContainer = hand.Container;
            if (handContainer == null || handContainer.ContainedEntity != null)
                return;

            if (!handContainer.Insert(entity))
            {
                Logger.Error($"{nameof(SharedHandsComponent)} on {uid} could not insert {entity} into {handContainer}.");
                return;
            }

            _adminLogSystem.Add(LogType.Pickup, LogImpact.Low, $"{ToPrettyString(uid):user} picked up {ToPrettyString(entity):entity}");

            hands.Dirty();

            var didEquip = new DidEquipHandEvent(uid, entity, hand);
            RaiseLocalEvent(uid, didEquip, false);

            var gotEquipped = new GotEquippedHandEvent(uid, entity, hand);
            RaiseLocalEvent(entity, gotEquipped);

            // TODO this should REALLY be a cancellable thing, not a handled event.
            // If one of the interactions resulted in the item being dropped, return early.
            if (gotEquipped.Handled)
                return;

            if (hand.Name == hands.ActiveHand)
                RaiseLocalEvent(entity, new HandSelectedEvent(uid, entity), false);
        }

        public abstract void PickupAnimation(EntityUid item, EntityCoordinates initialPosition, Vector2 finalPosition,
            EntityUid? exclude);

        protected virtual void HandleContainerRemoved(EntityUid uid, SharedHandsComponent component, ContainerModifiedMessage args)
        {
            HandleContainerModified(uid, component, args);
        }
        #endregion

        #region visuals
        private void HandleContainerModified(EntityUid uid, SharedHandsComponent hands, ContainerModifiedMessage args)
        {
            UpdateHandVisuals(uid, hands);
        }

        /// <summary>
        ///     Update the In-Hand sprites
        /// </summary>
        public virtual void UpdateHandVisuals(EntityUid uid, SharedHandsComponent? handComp = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref handComp, ref appearance, false))
                return;

            var handsVisuals = new List<HandVisualState>();
            foreach (var hand in handComp.Hands)
            {
                if (hand.HeldEntity == null)
                    continue;

                if (!TryComp(hand.HeldEntity.Value, out SharedItemComponent? item) || item.RsiPath == null)
                    continue;

                var handState = new HandVisualState(item.RsiPath, item.EquippedPrefix, hand.Location, item.Color);
                handsVisuals.Add(handState);
            }

            appearance.SetData(HandsVisuals.VisualState, new HandsVisualState(handsVisuals));
        }
        #endregion

        private void HandleSetHand(RequestSetHandEvent msg, EntitySessionEventArgs eventArgs)
        {
            if (eventArgs.SenderSession.AttachedEntity == null)
                return;

            TrySetActiveHand(eventArgs.SenderSession.AttachedEntity.Value, msg.HandName);
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
                RaiseLocalEvent(entity.Value, new HandDeselectedEvent(uid, entity.Value), false);

            handComp.ActiveHand = value;

            if (handComp.TryGetActiveHeldEntity(out entity))
                RaiseLocalEvent(entity.Value, new HandSelectedEvent(uid, entity.Value), false);

            handComp.Dirty();
            return true;
        }
    }
}
