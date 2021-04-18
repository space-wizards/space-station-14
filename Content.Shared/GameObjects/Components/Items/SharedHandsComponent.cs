#nullable enable
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.GameObjects.Components.Items
{
    public abstract class SharedHandsComponent : Component, ISharedHandsComponent
    {
        public sealed override string Name => "Hands";

        public sealed override uint? NetID => ContentNetIDs.HANDS;

        public event Action? OnItemChanged; //TODO: Try to replace C# event

        /// <summary>
        ///     The name of the currently active hand.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string? ActiveHand
        {
            get => _activeHand;
            set
            {
                if (value != null && !HasHand(value))
                {
                    Logger.Warning($"{nameof(SharedHandsComponent)} on {Owner} tried to set its active hand to {value}, which was not a hand.");
                    return;
                }
                if (value == null && Hands.Count != 0)
                {
                    Logger.Error($"{nameof(SharedHandsComponent)} on {Owner} tried to set its active hand to null, when it still had another hand.");
                    _activeHand = Hands[0].Name;
                    return;
                }
                if (value != ActiveHand)
                {
                    DeselectActiveHeldEntity();
                    _activeHand = value;
                    SelectActiveHeldEntity();

                    HandsModified();
                }
            }
        }
        private string? _activeHand;

        [ViewVariables]
        public IReadOnlyList<IReadOnlyHand> ReadOnlyHands => Hands;
        protected readonly List<Hand> Hands = new();

        /// <summary>
        ///     The amount of throw impulse per distance the player is from the throw target.
        /// </summary>
        [DataField("throwForceMultiplier")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ThrowForceMultiplier { get; set; } = 14f; //should be tuned so that a thrown item lands about under the player's cursor

        /// <summary>
        ///     Distance after which longer throw targets stop increasing throw impulse.
        /// </summary>
        [DataField("throwRange")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ThrowRange { get; set; } = 8f;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            var hands = new HandState[Hands.Count];

            for (var i = 0; i < Hands.Count; i++)
            {
                var hand = Hands[i].ToHandState();
                hands[i] = hand;
            }
            return new HandsComponentState(hands, ActiveHand);
        }

        public virtual void HandsModified()
        {
            Dirty();
        }

        public void AddHand(string handName, HandLocation handLocation)
        {
            if (HasHand(handName))
                return;

            var container = ContainerHelpers.CreateContainer<ContainerSlot>(Owner, handName);
            container.OccludesLight = false;

            Hands.Add(new Hand(handName, true, handLocation, container));

            if (ActiveHand == null)
                ActiveHand = handName;

            HandCountChanged();

            HandsModified();
        }

        public void RemoveHand(string handName)
        {
            if (!TryGetHand(handName, out var hand))
                return;

            RemoveHand(hand);
        }

        private void RemoveHand(Hand hand)
        {
            DropHeldEntityToFloor(hand, intentionalDrop: false);
            hand.Container?.Shutdown();
            Hands.Remove(hand);

            if (ActiveHand == hand.Name)
                ActiveHand = ReadOnlyHands.FirstOrDefault()?.Name;

            HandCountChanged();

            HandsModified();
        }

        public bool HasHand(string handName)
        {
            foreach (var hand in Hands)
            {
                if (hand.Name == handName)
                    return true;
            }
            return false;
        }

        private Hand? GetHand(string handName)
        {
            foreach (var hand in Hands)
            {
                if (hand.Name == handName)
                    return hand;
            }
            return null;
        }

        private Hand? GetActiveHand()
        {
            if (ActiveHand == null)
                return null;

            return GetHand(ActiveHand);
        }

        protected bool TryGetHand(string handName, [NotNullWhen(true)] out Hand? foundHand)
        {
            foundHand = GetHand(handName);
            return foundHand != null;
        }

        protected bool TryGetActiveHand([NotNullWhen(true)] out Hand? activeHand)
        {
            activeHand = GetActiveHand();
            return activeHand != null;
        }

        #region Held Entities

        public bool ActiveHandIsHoldingEntity()
        {
            if (!TryGetActiveHand(out var hand))
                return false;

            return hand.HeldEntity != null;
        }

        public bool TryGetHeldEntity(string handName, [NotNullWhen(true)] out IEntity? heldEntity)
        {
            heldEntity = null;

            if (!TryGetHand(handName, out var hand))
                return false;

            heldEntity = hand.HeldEntity;
            return heldEntity != null;
        }

        public bool TryGetActiveHeldEntity([NotNullWhen(true)] out IEntity? heldEntity)
        {
            heldEntity = GetActiveHand()?.HeldEntity;
            return heldEntity != null;
        }

        public bool IsHolding(IEntity entity)
        {
            foreach (var hand in Hands)
            {
                if (hand.HeldEntity == entity)
                    return true;
            }
            return false;
        }

        public IEnumerable<IEntity> GetAllHeldEntities()
        {
            foreach (var hand in ReadOnlyHands)
            {
                if (hand.HeldEntity != null)
                    yield return hand.HeldEntity;
            }
        }

        private bool TryGetHandHoldingEntity(IEntity entity, [NotNullWhen(true)] out Hand? handFound)
        {
            handFound = null;

            foreach (var hand in Hands)
            {
                if (hand.HeldEntity == entity)
                {
                    handFound = hand;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Dropping

        /// <summary>
        ///     Checks all the conditions relevant to a player being able to drop an item.
        /// </summary>
        public bool CanDrop(string handName, bool checkActionBlocker = true)
        {
            if (!TryGetHand(handName, out var hand))
                return false;

            if (!CanRemoveHeldEntityFromHand(hand))
                return false;

            if (checkActionBlocker && !PlayerCanDrop())
                return false;

            return true;
        }

        /// <summary>
        ///     Tries to drop the contents of the active hand to the target location.
        /// </summary>
        public bool TryDropActiveHand(EntityCoordinates targetDropLocation, bool doMobChecks = true, bool intentional = true)
        {
            if (!TryGetActiveHand(out var hand))
                return false;

            return TryDropHeldEntity(hand, targetDropLocation, doMobChecks, intentional);
        }

        /// <summary>
        ///     Tries to drop the contents of a hand to the target location.
        /// </summary>
        public bool TryDropHand(string handName, EntityCoordinates targetDropLocation, bool checkActionBlocker = true, bool intentional = true)
        {
            if (!TryGetHand(handName, out var hand))
                return false;

            return TryDropHeldEntity(hand, targetDropLocation, checkActionBlocker, intentional);
        }

        /// <summary>
        ///     Tries to drop a held entity to the target location.
        /// </summary>
        public bool TryDropEntity(IEntity entity, EntityCoordinates coords, bool doMobChecks = true, bool intentional = true)
        {
            if (!TryGetHandHoldingEntity(entity, out var hand))
                return false;

            return TryDropHeldEntity(hand, coords, doMobChecks, intentional);
        }

        /// <summary>
        ///     Attempts to move the contents of a hand into a container that is not another hand, without dropping it on the floor inbetween.
        /// </summary>
        public bool TryPutHandIntoContainer(string handName, BaseContainer targetContainer, bool checkActionBlocker = true)
        {
            if (!TryGetHand(handName, out var hand))
                return false;

            if (!CanPutHeldEntityIntoContainer(hand, targetContainer, checkActionBlocker))
                return false;

            PutHeldEntityIntoContainer(hand, targetContainer);
            return true;
        }

        /// <summary>
        ///     Attempts to move a held item from a hand into a container that is not another hand, without dropping it on the floor inbetween.
        /// </summary>
        public bool TryPutEntityIntoContainer(IEntity entity, BaseContainer targetContainer, bool checkActionBlocker = true)
        {
            if (!TryGetHandHoldingEntity(entity, out var hand))
                return false;

            if (!CanPutHeldEntityIntoContainer(hand, targetContainer, checkActionBlocker))
                return false;

            PutHeldEntityIntoContainer(hand, targetContainer);
            return true;
        }

        /// <summary>
        ///     Tries to drop the contents of a hand directly under the player.
        /// </summary>
        public bool TryDropHandToFloor(string handName, bool checkActionBlocker = true, bool intentionalDrop = true)
        {
            if (!TryGetHand(handName, out var hand))
                return false;

            return TryDropHeldEntity(hand, Owner.Transform.Coordinates, checkActionBlocker, intentionalDrop);
        }

        /// <summary>
        ///     Tries to drop a held entity directly under the player.
        /// </summary>
        public bool TryDropEntityToFloor(IEntity entity, bool checkActionBlocker = true, bool intentionalDrop = true)
        {
            if (!TryGetHandHoldingEntity(entity, out var hand))
                return false;

            return TryDropHeldEntity(hand, Owner.Transform.Coordinates, checkActionBlocker, intentionalDrop);
        }

        /// <summary>
        ///     Tries to remove the item in the active hand, without dropping it.
        ///     For transfering the held item to anothe rlocation, like an inventory slot,
        ///     which souldn't trigger the drop interaction
        public bool TryDropNoInteraction()
        {
            if (!TryGetActiveHand(out var hand))
                return false;

            if (!CanRemoveHeldEntityFromHand(hand))
                return false;

            RemoveHeldEntityFromHand(hand);
            return true;
        }

        /// <summary>
        ///     Checks if the contents of a hand is able to be removed from its container.
        /// </summary>
        private bool CanRemoveHeldEntityFromHand(Hand hand)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return false;

            var handContainer = hand.Container;
            if (handContainer == null)
                return false;

            if (!handContainer.CanRemove(heldEntity))
                return false;

            return true;
        }

        /// <summary>
        ///     Checks if the player is allowed to perform drops.
        /// </summary>
        private bool PlayerCanDrop()
        {
            if (!ActionBlockerSystem.CanDrop(Owner))
                return false;

            return true;
        }

        /// <summary>
        ///     Removes the contents of a hand from its container. Assumes that the removal is allowed.
        /// </summary>
        private void RemoveHeldEntityFromHand(Hand hand)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return;

            var handContainer = hand.Container;
            if (handContainer == null)
                return;

            if (hand.Name == ActiveHand)
                DeselectActiveHeldEntity();

            if (!handContainer.Remove(heldEntity))
            {
                Logger.Error($"{nameof(SharedHandsComponent)} on {Owner} could not remove {heldEntity} from {handContainer}.");
                return;
            }
            OnHeldEntityRemovedFromHand(heldEntity, hand.ToHandState());

            HandsModified();
        }

        /// <summary>
        ///     Drops a hands contents to the target location.
        /// </summary>
        private void DropHeldEntity(Hand hand, EntityCoordinates targetDropLocation, bool intentionalDrop = true)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return;

            RemoveHeldEntityFromHand(hand);

            DoDroppedInteraction(heldEntity, intentionalDrop);

            heldEntity.Transform.Coordinates = GetFinalDropCoordinates(targetDropLocation);

            OnItemChanged?.Invoke();
        }

        /// <summary>
        ///     Calculates the final location a dropped item will end up at, accounting for max drop range and collision along the targeted drop path.
        /// </summary>
        private EntityCoordinates GetFinalDropCoordinates(EntityCoordinates targetCoords)
        {
            var origin = Owner.Transform.MapPosition;
            var other = targetCoords.ToMap(Owner.EntityManager);

            var dropLength = EntitySystem.Get<SharedInteractionSystem>().UnobstructedDistance(origin, other, ignoredEnt: Owner);
            dropLength = MathF.Min(dropLength, SharedInteractionSystem.InteractionRange);

            var dropVector = origin.Position;
            if (dropLength != 0)
                dropVector += (other.Position - origin.Position).Normalized * dropLength;

            return targetCoords.WithPosition(dropVector);
        }

        /// <summary>
        ///     Tries to drop a hands contents to the target location.
        /// </summary>
        private bool TryDropHeldEntity(Hand hand, EntityCoordinates location, bool checkActionBlocker, bool intentionalDrop = true)
        {
            if (!CanRemoveHeldEntityFromHand(hand))
                return false;

            if (checkActionBlocker && !PlayerCanDrop())
                return false;

            DropHeldEntity(hand, location, intentionalDrop);
            return true;
        }

        /// <summary>
        ///     Drops the contents of a hand directly under the player.
        /// </summary>
        private void DropHeldEntityToFloor(Hand hand, bool intentionalDrop = true)
        {
            DropHeldEntity(hand, Owner.Transform.Coordinates, intentionalDrop);
        }

        private bool CanPutHeldEntityIntoContainer(Hand hand, IContainer targetContainer, bool checkActionBlocker)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return false;

            if (checkActionBlocker && !PlayerCanDrop())
                return false;

            if (!targetContainer.CanInsert(heldEntity))
                return false;

            return true;
        }

        /// <summary>
        ///     For putting the contents of a hand into a container that is not another hand.
        /// </summary>
        private void PutHeldEntityIntoContainer(Hand hand, IContainer targetContainer)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return;

            RemoveHeldEntityFromHand(hand);

            if (!targetContainer.Insert(heldEntity))
            {
                Logger.Error($"{nameof(SharedHandsComponent)} on {Owner} could not insert {heldEntity} into {targetContainer}.");
                return;
            }
        }

        #endregion

        #region Pickup

        public bool CanPickupEntity(string handName, IEntity entity, bool checkActionBlocker = true)
        {
            if (!TryGetHand(handName, out var hand))
                return false;

            if (checkActionBlocker && !PlayerCanPickup())
                return false;

            if (!CanInsertEntityIntoHand(hand, entity))
                return false;

            return true;
        }

        public bool CanPickupEntityToActiveHand(IEntity entity, bool checkActionBlocker = true)
        {
            if (!TryGetActiveHand(out var hand))
                return false;

            if (checkActionBlocker && !PlayerCanPickup())
                return false;

            if (!CanInsertEntityIntoHand(hand, entity))
                return false;

            return true;
        }

        /// <summary>
        ///     Tries to pick up an entity to a specific hand.
        /// </summary>
        public bool TryPickupEntity(string handName, IEntity entity, bool checkActionBlocker = true)
        {
            if (!TryGetHand(handName, out var hand))
                return false;

            return TryPickupEntity(hand, entity, checkActionBlocker);
        }

        public bool TryPickupEntityToActiveHand(IEntity entity, bool checkActionBlocker = true)
        {
            if (!TryGetActiveHand(out var hand))
                return false;

            return TryPickupEntity(hand, entity, checkActionBlocker);
        }

        /// <summary>
        ///     Checks if an entity can be put into a hand's container.
        /// </summary>
        protected bool CanInsertEntityIntoHand(Hand hand, IEntity entity)
        {
            if (!hand.Enabled)
                return false;

            var handContainer = hand.Container;
            if (handContainer == null)
                return false;

            if (!handContainer.CanInsert(entity))
                return false;

            return true;
        }

        /// <summary>
        ///     Checks if the player is allowed to perform pickup actions.
        /// </summary>
        /// <returns></returns>
        protected bool PlayerCanPickup()
        {
            if (!ActionBlockerSystem.CanPickup(Owner))
                return false;

            return true;
        }

        /// <summary>
        ///     Puts an entity into the player's hand, assumes that the insertion is allowed.
        /// </summary>
        private void PutEntityIntoHand(Hand hand, IEntity entity)
        {
            var handContainer = hand.Container;
            if (handContainer == null)
                return;

            if (!handContainer.Insert(entity))
            {
                Logger.Error($"{nameof(SharedHandsComponent)} on {Owner} could not insert {entity} into {handContainer}.");
                return;
            }

            DoEquippedHandInteraction(entity, hand.ToHandState());

            if (hand.Name == ActiveHand)
                SelectActiveHeldEntity();

            entity.Transform.LocalPosition = Vector2.Zero;

            OnItemChanged?.Invoke();

            HandsModified();
        }

        private bool TryPickupEntity(Hand hand, IEntity entity, bool checkActionBlocker = true)
        {
            if (!CanInsertEntityIntoHand(hand, entity))
                return false;

            if (checkActionBlocker && !PlayerCanPickup())
                return false;

            HandlePickupAnimation(entity);
            PutEntityIntoHand(hand, entity);
            return true;
        }

        #endregion

        #region Hand Interactions

        /// <summary>
        ///     Moves the active hand to the next hand.
        /// </summary>
        public void SwapHands()
        {
            if (!TryGetActiveHand(out var activeHand))
                return;

            var newActiveIndex = Hands.IndexOf(activeHand) + 1;
            var finalHandIndex = Hands.Count - 1;
            if (newActiveIndex > finalHandIndex)
                newActiveIndex = 0;

            ActiveHand = ReadOnlyHands[newActiveIndex].Name;
        }

        /// <summary>
        ///     Attempts to interact with the item in a hand using the active held item.
        /// </summary>
        public void InteractHandWithActiveHand(string handName)
        {
            if (!TryGetActiveHeldEntity(out var activeHeldEntity))
                return;

            if (!TryGetHeldEntity(handName, out var heldEntity))
                return;

            if (activeHeldEntity == heldEntity)
                return;

           DoInteraction(activeHeldEntity, heldEntity);
        }

        public void UseActiveHeldEntity()
        {
            if (!TryGetActiveHeldEntity(out var heldEntity))
                return;

            DoUse(heldEntity);
        }

        public void ActivateHeldEntity(string handName)
        {
            if (!TryGetHeldEntity(handName, out var heldEntity))
                return;

            DoActivate(heldEntity);
        }

        /// <summary>
        ///     Moves an entity from one hand to the active hand.
        /// </summary>
        public bool TryMoveHeldEntityToActiveHand(string handName, bool checkActionBlocker = true)
        {
            if (!TryGetHand(handName, out var hand) || !TryGetActiveHand(out var activeHand))
                return false;

            if (!TryGetHeldEntity(handName, out var heldEntity))
                return false;

            if (!CanInsertEntityIntoHand(activeHand, heldEntity) || !CanRemoveHeldEntityFromHand(hand))
                return false;

            if (checkActionBlocker && (!PlayerCanDrop() || !PlayerCanPickup()))
                return false;

            RemoveHeldEntityFromHand(hand);
            PutEntityIntoHand(activeHand, heldEntity);
            return true;
        }

        #endregion

        private void DeselectActiveHeldEntity()
        {
            if (TryGetActiveHeldEntity(out var entity))
                DoHandDeselectedInteraction(entity);
        }

        private void SelectActiveHeldEntity()
        {
            if (TryGetActiveHeldEntity(out var entity))
                DoHandSelectedInteraction(entity);
        }

        private void HandCountChanged()
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new HandCountChangedEvent(Owner));
        }

        /// <summary>
        ///     Tries to pick up an entity into the active hand. If it cannot, tries to pick up the entity into each other hand.
        /// </summary>
        public bool TryPutInActiveHandOrAny(IEntity entity, bool checkActionBlocker = true)
        {
            return TryPutInAnyHand(entity, GetActiveHand(), checkActionBlocker);
        }

        /// <summary>
        ///     Tries to pick up an entity into the priority hand, if provided. If it cannot, tries to pick up the entity into each other hand.
        /// </summary>
        public bool TryPutInAnyHand(IEntity entity, string? priorityHandName = null, bool checkActionBlocker = true)
        {
            Hand? priorityHand = null;

            if (priorityHandName != null)
                priorityHand = GetHand(priorityHandName);

            return TryPutInAnyHand(entity, priorityHand, checkActionBlocker);
        }

        /// <summary>
        ///     Tries to pick up an entity into the priority hand, if provided. If it cannot, tries to pick up the entity into each other hand.
        /// </summary>
        private bool TryPutInAnyHand(IEntity entity, Hand? priorityHand = null, bool checkActionBlocker = true)
        {
            if (priorityHand != null)
            {
                if (TryPickupEntity(priorityHand, entity, checkActionBlocker))
                    return true;
            }

            foreach (var hand in Hands)
            {
                if (TryPickupEntity(hand, entity, checkActionBlocker))
                    return true;
            }
            return false;
        }

        protected virtual void OnHeldEntityRemovedFromHand(IEntity heldEntity, HandState handState) { }

        protected virtual void DoDroppedInteraction(IEntity heldEntity, bool intentionalDrop) { }

        protected virtual void DoEquippedHandInteraction(IEntity entity, HandState handState) { }

        protected virtual void DoHandSelectedInteraction(IEntity entity) { }

        protected virtual void DoHandDeselectedInteraction(IEntity entity) { }

        protected virtual void DoInteraction(IEntity activeHeldEntity, IEntity heldEntity) { }

        protected virtual void DoUse(IEntity heldEntity) { }

        protected virtual void DoActivate(IEntity heldEntity) { }

        protected virtual void HandlePickupAnimation(IEntity entity) { }

        protected void EnableHand(Hand hand)
        {
            hand.Enabled = true;
            Dirty();
        }

        protected void DisableHand(Hand hand)
        {
            hand.Enabled = false;
            DropHeldEntityToFloor(hand, intentionalDrop: false);
            Dirty();
        }
    }

    public interface IReadOnlyHand
    {
        public string Name { get; }

        public bool Enabled { get; }

        public HandLocation Location { get; }

        public abstract IEntity? HeldEntity { get; }
    }

    public class Hand : IReadOnlyHand
    {
        [ViewVariables]
        public string Name { get; set; }

        [ViewVariables]
        public bool Enabled { get; set; }

        [ViewVariables]
        public HandLocation Location { get; set; }

        /// <summary>
        ///     The container used to hold the contents of this hand. Nullable because the client must get the containers via <see cref="ContainerManagerComponent"/>,
        ///     which may not be synced with the server when the client hands are created.
        /// </summary>
        [ViewVariables]
        public IContainer? Container { get; set; }

        [ViewVariables]
        public IEntity? HeldEntity => Container?.ContainedEntities?.FirstOrDefault();

        public Hand(string name, bool enabled, HandLocation location, IContainer? container = null)
        {
            Name = name;
            Enabled = enabled;
            Location = location;
            Container = container;
        }

        public HandState ToHandState()
        {
            return new(Name, Location, Enabled);
        }
    }

    [Serializable, NetSerializable]
    public sealed class HandState
    {
        public string Name { get; }
        public HandLocation Location { get; }
        public bool Enabled { get; }

        public HandState(string name, HandLocation location, bool enabled)
        {
            Name = name;
            Location = location;
            Enabled = enabled;
        }
    }

    [Serializable, NetSerializable]
    public class HandsComponentState : ComponentState
    {
        public HandState[] Hands { get; }
        public string? ActiveHand { get; }

        public HandsComponentState(HandState[] hands, string? activeHand = null) : base(ContentNetIDs.HANDS)
        {
            Hands = hands;
            ActiveHand = activeHand;
        }
    }

    /// <summary>
    /// A message that calls the use interaction on an item in hand, presumed for now the interaction will occur only on the active hand.
    /// </summary>
    [Serializable, NetSerializable]
    public class UseInHandMsg : ComponentMessage
    {
        public UseInHandMsg()
        {
            Directed = true;
        }
    }

    /// <summary>
    /// A message that calls the activate interaction on the item in the specified hand.
    /// </summary>
    [Serializable, NetSerializable]
    public class ActivateInHandMsg : ComponentMessage
    {
        public string HandName { get; }

        public ActivateInHandMsg(string handName)
        {
            Directed = true;
            HandName = handName;
        }
    }

    /// <summary>
    ///     Uses the item in the active hand on the item in the specified hand.
    /// </summary>
    [Serializable, NetSerializable]
    public class ClientAttackByInHandMsg : ComponentMessage
    {
        public string HandName { get; }

        public ClientAttackByInHandMsg(string handName)
        {
            Directed = true;
            HandName = handName;
        }
    }

    /// <summary>
    ///     Moves an item from one hand to the active hand.
    /// </summary>
    [Serializable, NetSerializable]
    public class MoveItemFromHandMsg : ComponentMessage
    {
        public string HandName { get; }

        public MoveItemFromHandMsg(string handName)
        {
            Directed = true;
            HandName = handName;
        }
    }

    /// <summary>
    ///     Sets the player's active hand to a specified hand.
    /// </summary>
    [Serializable, NetSerializable]
    public class ClientChangedHandMsg : ComponentMessage
    {
        public string HandName { get; }

        public ClientChangedHandMsg(string handName)
        {
            Directed = true;
            HandName = handName;
        }
    }

    /// <summary>
    ///     What side of the body this hand is on.
    /// </summary>
    public enum HandLocation : byte
    {
        Left,
        Middle,
        Right
    }

    public class HandCountChangedEvent : EntityEventArgs
    {
        public HandCountChangedEvent(IEntity sender)
        {
            Sender = sender;
        }

        public IEntity Sender { get; }
    }

    [Serializable, NetSerializable]
    public class PickupAnimationMessage : ComponentMessage
    {
        public EntityUid EntityUid { get; }
        public EntityCoordinates InitialPosition { get; }
        public Vector2 PickupDirection { get; }

        public PickupAnimationMessage(EntityUid entityUid, Vector2 pickupDirection, EntityCoordinates initialPosition)
        {
            Directed = true;
            EntityUid = entityUid;
            PickupDirection = pickupDirection;
            InitialPosition = initialPosition;
        }
    }
}
