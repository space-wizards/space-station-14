using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Hands.Components
{
    [NetworkedComponent]
    public abstract class SharedHandsComponent : Component
    {
        public sealed override string Name => "Hands";

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
        public readonly List<Hand> Hands = new();

        /// <summary>
        ///     The amount of throw impulse per distance the player is from the throw target.
        /// </summary>
        [DataField("throwForceMultiplier")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ThrowForceMultiplier { get; set; } = 10f; //should be tuned so that a thrown item lands about under the player's cursor

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
            // todo axe all this for ECS.
            // todo burn it all down.
            UpdateHandVisualizer();
            Dirty();

            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new HandsModifiedMessage { Hands = this });
        }

        public void UpdateHandVisualizer()
        {
            if (!Owner.TryGetComponent(out SharedAppearanceComponent? appearance))
                return;

            var hands = new List<HandVisualState>();
            foreach (var hand in Hands)
            {
                if (hand.HeldEntity == null)
                    continue;

                if (!hand.HeldEntity.TryGetComponent(out SharedItemComponent? item) || item.RsiPath == null)
                    continue;

                var handState = new HandVisualState(item.RsiPath, item.EquippedPrefix, hand.Location, item.Color);
                hands.Add(handState);
            }

            appearance.SetData(HandsVisuals.VisualState, new HandsVisualState(hands));
        }

        public void AddHand(string handName, HandLocation handLocation)
        {
            if (HasHand(handName))
                return;

            var container = Owner.CreateContainer<ContainerSlot>(handName);
            container.OccludesLight = false;

            Hands.Add(new Hand(handName, handLocation, container));

            ActiveHand ??= handName;

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
                ActiveHand = Hands.FirstOrDefault()?.Name;

            HandCountChanged();

            HandsModified();
        }

        private Hand? GetActiveHand()
        {
            if (ActiveHand == null)
                return null;

            return GetHandOrNull(ActiveHand);
        }

        public bool HasHand(string handName)
        {
            return TryGetHand(handName, out _);
        }

        public Hand? GetHandOrNull(string handName)
        {
            return TryGetHand(handName, out var hand) ? hand : null;
        }

        public Hand GetHand(string handName)
        {
            if (!TryGetHand(handName, out var hand))
                throw new KeyNotFoundException($"Unable to find hand with name {handName}");

            return hand;
        }

        public bool TryGetHand(string handName, [NotNullWhen(true)] out Hand? foundHand)
        {
            foreach (var hand in Hands)
            {
                if (hand.Name == handName)
                {
                    foundHand = hand;
                    return true;
                };
            }

            foundHand = null;
            return false;
        }

        public bool TryGetActiveHand([NotNullWhen(true)] out Hand? activeHand)
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
            foreach (var hand in Hands)
            {
                if (hand.HeldEntity != null)
                    yield return hand.HeldEntity;
            }
        }

        /// <summary>
        ///     Returns the number of hands that have no items in them.
        /// </summary>
        /// <returns></returns>
        public int GetFreeHands()
        {
            int acc = 0;
            foreach (var hand in Hands)
            {
                if (hand.HeldEntity == null)
                    acc += 1;
            }

            return acc;
        }

        public bool TryGetHandHoldingEntity(IEntity entity, [NotNullWhen(true)] out Hand? handFound)
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
        ///     Attempts to move a held item from a hand into a container that is not another hand, without dropping it on the floor in-between.
        /// </summary>
        public bool Drop(IEntity entity, BaseContainer targetContainer, bool checkActionBlocker = true)
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
        public bool Drop(string handName, bool checkActionBlocker = true, bool intentionalDrop = true)
        {
            if (!TryGetHand(handName, out var hand))
                return false;

            return TryDropHeldEntity(hand, Owner.Transform.Coordinates, checkActionBlocker, intentionalDrop);
        }

        /// <summary>
        ///     Tries to drop a held entity directly under the player.
        /// </summary>
        public bool Drop(IEntity entity, bool checkActionBlocker = true, bool intentionalDrop = true)
        {
            if (!TryGetHandHoldingEntity(entity, out var hand))
                return false;

            return TryDropHeldEntity(hand, Owner.Transform.Coordinates, checkActionBlocker, intentionalDrop);
        }

        /// <summary>
        ///     Tries to remove the item in the active hand, without dropping it.
        ///     For transferring the held item to another location, like an inventory slot,
        ///     which shouldn't trigger the drop interaction
        /// </summary>
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
            if (!IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ActionBlockerSystem>().CanDrop(OwnerUid))
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
        public void DropHeldEntity(Hand hand, EntityCoordinates targetDropLocation, bool intentionalDrop = true)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return;

            RemoveHeldEntityFromHand(hand);

            EntitySystem.Get<SharedInteractionSystem>().DroppedInteraction(Owner, heldEntity, intentionalDrop);

            heldEntity.Transform.WorldPosition = GetFinalDropCoordinates(targetDropLocation);

            OnItemChanged?.Invoke();
        }

        /// <summary>
        ///     Calculates the final location a dropped item will end up at, accounting for max drop range and collision along the targeted drop path.
        /// </summary>
        private Vector2 GetFinalDropCoordinates(EntityCoordinates targetCoords)
        {
            var origin = Owner.Transform.MapPosition;
            var target = targetCoords.ToMap(Owner.EntityManager);

            var dropVector = target.Position - origin.Position;
            var requestedDropDistance = dropVector.Length;

            if (dropVector.Length > SharedInteractionSystem.InteractionRange)
            {
                dropVector = dropVector.Normalized * SharedInteractionSystem.InteractionRange;
                target = new MapCoordinates(origin.Position + dropVector, target.MapId);
            }

            var dropLength = EntitySystem.Get<SharedInteractionSystem>().UnobstructedDistance(origin, target, ignoredEnt: Owner);

            if (dropLength < requestedDropDistance)
                return origin.Position + dropVector.Normalized * dropLength;
            return target.Position;
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
            return ActiveHand != null && CanPickupEntity(ActiveHand, entity, checkActionBlocker);
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
            return ActiveHand != null && TryPickupEntity(ActiveHand, entity, checkActionBlocker);
        }

        /// <summary>
        ///     Checks if an entity can be put into a hand's container.
        /// </summary>
        protected bool CanInsertEntityIntoHand(Hand hand, IEntity entity)
        {
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
            if (!EntitySystem.Get<ActionBlockerSystem>().CanPickup(Owner.Uid))
                return false;

            return true;
        }

        /// <summary>
        ///     Puts an entity into the player's hand, assumes that the insertion is allowed.
        /// </summary>
        public void PutEntityIntoHand(Hand hand, IEntity entity)
        {
            var handContainer = hand.Container;
            if (handContainer == null)
                return;

            if (!handContainer.Insert(entity))
            {
                Logger.Error($"{nameof(SharedHandsComponent)} on {Owner} could not insert {entity} into {handContainer}.");
                return;
            }

            EntitySystem.Get<SharedInteractionSystem>().EquippedHandInteraction(Owner, entity, hand.ToHandState());

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
        ///     Get the name of the hand that a swap hands would result in.
        /// </summary>
        public bool TryGetSwapHandsResult([NotNullWhen(true)] out string? nextHand)
        {
            nextHand = null;

            if (!TryGetActiveHand(out var activeHand) || Hands.Count == 1)
                return false;

            var newActiveIndex = Hands.IndexOf(activeHand) + 1;
            var finalHandIndex = Hands.Count - 1;
            if (newActiveIndex > finalHandIndex)
                newActiveIndex = 0;

            nextHand = Hands[newActiveIndex].Name;
            return true;
        }

        /// <summary>
        ///     Attempts to interact with the item in a hand using the active held item.
        /// </summary>
        public async void InteractHandWithActiveHand(string handName)
        {
            if (!TryGetActiveHeldEntity(out var activeHeldEntity))
                return;

            if (!TryGetHeldEntity(handName, out var heldEntity))
                return;

            if (activeHeldEntity == heldEntity)
                return;

            await EntitySystem.Get<SharedInteractionSystem>()
                .InteractUsing(Owner, activeHeldEntity, heldEntity, EntityCoordinates.Invalid);
        }

        public void ActivateItem(bool altInteract = false)
        {
            if (!TryGetActiveHeldEntity(out var heldEntity))
                return;

            EntitySystem.Get<SharedInteractionSystem>()
                .TryUseInteraction(Owner, heldEntity, altInteract);
        }

        public void ActivateHeldEntity(string handName)
        {
            if (!TryGetHeldEntity(handName, out var heldEntity))
                return;

            EntitySystem.Get<SharedInteractionSystem>()
                .TryInteractionActivate(Owner, heldEntity);
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
                EntitySystem.Get<SharedInteractionSystem>().HandDeselectedInteraction(Owner, entity);
        }

        private void SelectActiveHeldEntity()
        {
            if (TryGetActiveHeldEntity(out var entity))
                EntitySystem.Get<SharedInteractionSystem>().HandSelectedInteraction(Owner, entity);
        }

        private void HandCountChanged()
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new HandCountChangedEvent(Owner));
        }

        /// <summary>
        ///     Tries to pick up an entity into the active hand. If it cannot, tries to pick up the entity into each other hand.
        /// </summary>
        public bool PutInHand(SharedItemComponent item, bool checkActionBlocker = true)
        {
            return TryPutInActiveHandOrAny(item.Owner, checkActionBlocker);
        }

        /// <summary>
        ///     Puts an item any hand, prefering the active hand, or puts it on the floor under the player.
        /// </summary>
        public void PutInHandOrDrop(SharedItemComponent item, bool checkActionBlocker = true)
        {
            var entity = item.Owner;

            if (!TryPutInActiveHandOrAny(entity, checkActionBlocker))
                entity.Transform.Coordinates = Owner.Transform.Coordinates;
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
                priorityHand = GetHandOrNull(priorityHandName);

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

        protected virtual void HandlePickupAnimation(IEntity entity) { }
    }

    #region visualizerData
    [Serializable, NetSerializable]
    public enum HandsVisuals : byte
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public class HandsVisualState
    {
        public List<HandVisualState> Hands { get; } = new();

        public HandsVisualState(List<HandVisualState> hands)
        {
            Hands = hands;
        }
    }

    [Serializable, NetSerializable]
    public class HandVisualState
    {
        public string RsiPath { get; }
        public string? EquippedPrefix { get; }
        public HandLocation Location { get; }
        public Color Color { get; }

        public HandVisualState(string rsiPath, string? equippedPrefix, HandLocation location, Color color)
        {
            RsiPath = rsiPath;
            EquippedPrefix = equippedPrefix;
            Location = location;
            Color = color;
        }
    }
    #endregion

    public class Hand
    {
        [ViewVariables]
        public string Name { get; }

        [ViewVariables]
        public HandLocation Location { get; }

        /// <summary>
        ///     The container used to hold the contents of this hand. Nullable because the client must get the containers via <see cref="ContainerManagerComponent"/>,
        ///     which may not be synced with the server when the client hands are created.
        /// </summary>
        [ViewVariables]
        public IContainer? Container { get; set; }

        [ViewVariables]
        public IEntity? HeldEntity => Container?.ContainedEntities?.FirstOrDefault();

        public bool IsEmpty => HeldEntity == null;

        public Hand(string name, HandLocation location, IContainer? container = null)
        {
            Name = name;
            Location = location;
            Container = container;
        }

        public HandState ToHandState()
        {
            return new(Name, Location);
        }
    }

    [Serializable, NetSerializable]
    public struct HandState
    {
        public string Name { get; }
        public HandLocation Location { get; }

        public HandState(string name, HandLocation location)
        {
            Name = name;
            Location = location;
        }
    }

    [Serializable, NetSerializable]
    public sealed class HandsComponentState : ComponentState
    {
        public HandState[] Hands { get; }
        public string? ActiveHand { get; }

        public HandsComponentState(HandState[] hands, string? activeHand = null)
        {
            Hands = hands;
            ActiveHand = activeHand;
        }
    }

    /// <summary>
    /// A message that calls the use interaction on an item in hand, presumed for now the interaction will occur only on the active hand.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class UseInHandMsg : EntityEventArgs
    {
    }

    /// <summary>
    /// A message that calls the activate interaction on the item in the specified hand.
    /// </summary>
    [Serializable, NetSerializable]
    public class ActivateInHandMsg : EntityEventArgs
    {
        public string HandName { get; }

        public ActivateInHandMsg(string handName)
        {
            HandName = handName;
        }
    }

    /// <summary>
    ///     Uses the item in the active hand on the item in the specified hand.
    /// </summary>
    [Serializable, NetSerializable]
    public class ClientInteractUsingInHandMsg : EntityEventArgs
    {
        public string HandName { get; }

        public ClientInteractUsingInHandMsg(string handName)
        {
            HandName = handName;
        }
    }

    /// <summary>
    ///     Moves an item from one hand to the active hand.
    /// </summary>
    [Serializable, NetSerializable]
    public class MoveItemFromHandMsg : EntityEventArgs
    {
        public string HandName { get; }

        public MoveItemFromHandMsg(string handName)
        {
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
    public class PickupAnimationMessage : EntityEventArgs
    {
        public EntityUid EntityUid { get; }
        public EntityCoordinates InitialPosition { get; }
        public Vector2 FinalPosition { get; }

        public PickupAnimationMessage(EntityUid entityUid, Vector2 finalPosition, EntityCoordinates initialPosition)
        {
            EntityUid = entityUid;
            FinalPosition = finalPosition;
            InitialPosition = initialPosition;
        }
    }

    [Serializable, NetSerializable]
    public struct HandsModifiedMessage
    {
        public SharedHandsComponent Hands;
    }
}
