#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Items
{
    public abstract class SharedHandsComponent : Component, ISharedHandsComponent
    {
        public sealed override string Name => "Hands";

        public sealed override uint? NetID => ContentNetIDs.HANDS;

        public event Action? OnItemChanged;

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
                    Dirty();
                }
            }
        }
        private string? _activeHand;

        [ViewVariables]
        public IReadOnlyList<IReadOnlyHand> ReadOnlyHands => Hands;
        protected readonly List<Hand> Hands = new();

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
            Dirty();
        }

        public void RemoveHand(string handName)
        {
            if (!TryGetHand(handName, out var hand))
                return;

            RemoveHand(hand);
        }

        protected void RemoveHand(Hand hand)
        {
            DropHeldEntityToFloor(hand, intentionalDrop: false);
            hand.Container.Shutdown();
            Hands.Remove(hand);

            if (ActiveHand == hand.Name)
                ActiveHand = ReadOnlyHands.FirstOrDefault()?.Name;

            HandCountChanged();
            Dirty();
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

        protected Hand? GetServerHand(string handName)
        {
            foreach (var hand in Hands)
            {
                if (hand.Name == handName)
                    return hand;
            }
            return null;
        }

        protected Hand? GetActiveHand()
        {
            if (ActiveHand == null)
                return null;

            return GetServerHand(ActiveHand);
        }

        protected bool TryGetHand(string handName, [NotNullWhen(true)] out Hand? foundHand)
        {
            foundHand = GetServerHand(handName);
            return foundHand != null;
        }

        protected bool TryGetActiveHand([NotNullWhen(true)] out Hand? activeHand)
        {
            activeHand = GetActiveHand();
            return activeHand != null;
        }

        #region Held Entities

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

        protected bool TryGetHandHoldingEntity(IEntity entity, [NotNullWhen(true)] out Hand? handFound)
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

        public bool Drop(string handName, EntityCoordinates targetDropLocation, bool doMobChecks = true, bool intentional = true)
        {
            if (!TryGetHand(handName, out var hand))
                return false;

            return TryDropHeldEntity(hand, targetDropLocation, doMobChecks, intentional);
        }

        public bool Drop(IEntity entity, EntityCoordinates coords, bool doMobChecks = true, bool intentional = true)
        {
            if (!TryGetHandHoldingEntity(entity, out var hand))
                return false;

            return TryDropHeldEntity(hand, coords, doMobChecks, intentional);
        }

        public bool TryPutHandIntoContainer(string handName, BaseContainer targetContainer, bool doMobChecks = true, bool intentional = true)
        {
            if (!TryGetHand(handName, out var hand))
                return false;

            if (!CanPutHeldEntityIntoContainer(hand, targetContainer, doMobChecks))
                return false;

            PutHeldEntityIntoContainer(hand, targetContainer);
            return true;
        }

        public bool TryPutEntityIntoContainer(IEntity entity, BaseContainer targetContainer, bool checkActionBlocker = true)
        {
            if (!TryGetHandHoldingEntity(entity, out var hand))
                return false;

            if (!CanPutHeldEntityIntoContainer(hand, targetContainer, checkActionBlocker))
                return false;

            PutHeldEntityIntoContainer(hand, targetContainer);
            return true;
        }

        public bool TryDropHandToFloor(string handName, bool checkActionBlocker = true, bool intentionalDrop = true)
        {
            if (!TryGetHand(handName, out var hand))
                return false;

            return TryDropHeldEntity(hand, Owner.Transform.Coordinates, checkActionBlocker, intentionalDrop);
        }

        public bool TryDropEntityToFloor(IEntity entity, bool checkActionBlocker = true, bool intentionalDrop = true)
        {
            if (!TryGetHandHoldingEntity(entity, out var hand))
                return false;

            return TryDropHeldEntity(hand, Owner.Transform.Coordinates, checkActionBlocker, intentionalDrop);
        }

        /// <summary>
        ///     Tries to remove the item from the active hand without triggering <see cref="IDropped"/>.
        /// </summary>
        public bool TryDropActiveHeldItemForEquip()
        {
            if (!TryGetActiveHand(out var hand))
                return false;

            if (!CanRemoveHeldEntityFromHand(hand))
                return false;

            RemoveHeldEntityFromHand(hand);
            return true;
        }

        protected bool CanRemoveHeldEntityFromHand(Hand hand)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return false;

            if (!hand.Container.CanRemove(heldEntity))
                return false;

            return true;
        }

        protected bool PlayerCanDrop()
        {
            if (!ActionBlockerSystem.CanDrop(Owner))
                return false;

            return true;
        }

        protected void RemoveHeldEntityFromHand(Hand hand)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return;

            if (hand.Name == ActiveHand)
                DeselectActiveHeldEntity();

            if (!hand.Container.Remove(heldEntity))
            {
                Logger.Error($"{nameof(SharedHandsComponent)} on {Owner} could not remove {heldEntity} from {hand.Container}.");
                return;
            }
            OnHeldEntityRemovedFromHand(heldEntity, hand.ToHandState());

        }

        protected void DropHeldEntity(Hand hand, EntityCoordinates targetDropLocation, bool intentionalDrop)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return;

            RemoveHeldEntityFromHand(hand);

            DoDroppedInteraction(heldEntity, intentionalDrop);

            heldEntity.Transform.Coordinates = GetFinalDropCoordinates(targetDropLocation);

            OnItemChanged?.Invoke();
            Dirty();
        }

        /// <summary>
        ///     Calculates the final location a dropped item will end up at, accounting for max drop range and collision along the targeted drop path.
        /// </summary>
        protected EntityCoordinates GetFinalDropCoordinates(EntityCoordinates targetCoords)
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

        protected bool TryDropHeldEntity(Hand hand, EntityCoordinates location, bool checkActionBlocker, bool intentionalDrop)
        {
            if (!CanRemoveHeldEntityFromHand(hand))
                return false;

            if (checkActionBlocker && !PlayerCanDrop())
                return false;

            DropHeldEntity(hand, location, intentionalDrop);
            return true;
        }

        /// <summary>
        ///     Forcibly drops the contents of a hand directly under the player.
        /// </summary>
        protected void DropHeldEntityToFloor(Hand hand, bool intentionalDrop)
        {
            DropHeldEntity(hand, Owner.Transform.Coordinates, intentionalDrop);
        }

        protected bool CanPutHeldEntityIntoContainer(Hand hand, IContainer targetContainer, bool checkActionBlocker)
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

        protected void PutHeldEntityIntoContainer(Hand hand, IContainer targetContainer)
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
            Dirty();
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

        protected bool CanInsertEntityIntoHand(Hand hand, IEntity entity)
        {
            if (!hand.Container.CanInsert(entity))
                return false;

            return true;
        }

        protected bool PlayerCanPickup()
        {
            if (!ActionBlockerSystem.CanPickup(Owner))
                return false;

            return true;
        }

        protected void PutEntityIntoHand(Hand hand, IEntity entity)
        {
            if (!hand.Container.Insert(entity))
            {
                Logger.Error($"{nameof(SharedHandsComponent)} on {Owner} could not insert {entity} into {hand.Container}.");
                return;
            }

            DoEquippedHandInteraction(entity, hand.ToHandState());

            if (hand.Name == ActiveHand)
                SelectActiveHeldEntity();

            entity.Transform.LocalPosition = Vector2.Zero;

            OnItemChanged?.Invoke();
            Dirty();

            var entityPosition = entity.TryGetContainer(out var container) ? container.Owner.Transform.Coordinates : entity.Transform.Coordinates;

            if (entityPosition != Owner.Transform.Coordinates)
            {
                SendNetworkMessage(new AnimatePickupEntityMessage(entity.Uid, entityPosition));
            }
        }

        protected bool TryPickupEntity(Hand hand, IEntity entity, bool checkActionBlocker = true)
        {
            if (!CanInsertEntityIntoHand(hand, entity))
                return false;

            if (checkActionBlocker && !PlayerCanPickup())
                return false;

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
            if (!TryGetHand(handName, out var hand))
                return false;

            if (!TryGetHeldEntity(handName, out var heldEntity))
                return false;

            if (!TryGetActiveHand(out var activeHand) || activeHand.HeldEntity != null)
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
        ///     Tries to pick up an entity into the active hand. If it cannot, tries to pick up the entity into every other hand.
        /// </summary>
        public bool TryPutInActiveHandOrAny(IEntity entity, bool checkActionBlocker = true)
        {
            return TryPutInAnyHand(entity, GetActiveHand(), checkActionBlocker);
        }

        /// <summary>
        ///     Tries to pick up an entity into the priority hand, if provided. If it cannot, tries to pick up the entity into every other hand.
        /// </summary>
        public bool TryPutInAnyHand(IEntity entity, string? priorityHandName = null, bool checkActionBlocker = true)
        {
            Hand? priorityHand = null;

            if (priorityHandName != null)
                priorityHand = GetServerHand(priorityHandName);

            return TryPutInAnyHand(entity, priorityHand, checkActionBlocker);
        }

        /// <summary>
        ///     Tries to pick up an entity into the priority hand, if provided. Then, tries to pick up the entity into every other hand.
        /// </summary>
        protected bool TryPutInAnyHand(IEntity entity, Hand? priorityHand = null, bool checkActionBlocker = true)
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

        [ViewVariables]
        public IContainer Container { get; }

        [ViewVariables]
        public IEntity? HeldEntity => Container.ContainedEntities.FirstOrDefault();

        public Hand(string name, bool enabled, HandLocation location, IContainer container)
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
    /// A message that calls the activate interaction on the item in Index.
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

    [Serializable, NetSerializable]
    public class HandEnabledMsg : ComponentMessage
    {
        public string Name { get; }

        public HandEnabledMsg(string name)
        {
            Name = name;
        }
    }

    [Serializable, NetSerializable]
    public class HandDisabledMsg : ComponentMessage
    {
        public string Name { get; }

        public HandDisabledMsg(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    ///     Whether a hand is a left or right hand, or some other type of hand.
    /// </summary>
    public enum HandLocation : byte
    {
        Left,
        Middle,
        Right
    }

    /// <summary>
    /// Component message for displaying an animation of an entity flying towards the owner of a HandsComponent
    /// </summary>
    [Serializable, NetSerializable]
    public class AnimatePickupEntityMessage : ComponentMessage
    {
        public readonly EntityUid EntityId;
        public readonly EntityCoordinates EntityPosition;
        public AnimatePickupEntityMessage(EntityUid entity, EntityCoordinates entityPosition)
        {
            Directed = true;
            EntityId = entity;
            EntityPosition = entityPosition;
        }
    }

    public class HandCountChangedEvent : EntityEventArgs
    {
        public HandCountChangedEvent(IEntity sender)
        {
            Sender = sender;
        }

        public IEntity Sender { get; }
    }
}
