#nullable enable
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Pulling;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics.Pull;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.GameObjects.Components.GUI
{
    [RegisterComponent]
    [ComponentReference(typeof(IHandsComponent))]
    [ComponentReference(typeof(ISharedHandsComponent))]
    [ComponentReference(typeof(SharedHandsComponent))]
    public class HandsComponent : SharedHandsComponent, IHandsComponent, IBodyPartAdded, IBodyPartRemoved, IDisarmedAct
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        public event Action? OnItemChanged;

        [ViewVariables(VVAccess.ReadWrite)]
        public string? ActiveHand
        {
            get => _activeHand;
            set
            {
                if (value != null && !HasHand(value))
                {
                    Logger.Warning($"{nameof(HandsComponent)} on {Owner} tried to set its active hand to {value}, which was not a hand.");
                    return;
                }
                _activeHand = value;
                Dirty();
            }
        }
        private string? _activeHand;

        [ViewVariables]
        public IReadOnlyList<IReadOnlyHand> ReadOnlyHands => _hands;
        private readonly List<SharedHand> _hands = new();

        protected override void Startup()
        {
            base.Startup();
            ActiveHand = _hands.FirstOrDefault()?.Name;
            Dirty();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            var hands = new HandState[_hands.Count];

            for (var i = 0; i < _hands.Count; i++)
            {
                var hand = _hands[i].ToHandState();
                hands[i] = hand;
            }
            return new HandsComponentState(hands, ActiveHand);
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PullAttemptMessage msg:
                    AttemptPull(msg);
                    break;
                case PullStartedMessage:
                    StartPulling();
                    break;
                case PullStoppedMessage:
                    StopPulling();
                    break;
                case HandDisabledMsg msg:
                    Drop(msg.Name, false);
                    break;
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                case ClientChangedHandMsg msg:
                    ActiveHand = msg.HandName;
                    break;
                case ClientAttackByInHandMsg msg:
                    InteractHandWithActiveHand(msg.HandName);
                    break;
                case UseInHandMsg:
                    UseActiveHeldEntity();
                    break;
                case ActivateInHandMsg msg:
                    ActivateHeldEntity(msg.HandName);
                    break;
                case MoveItemFromHandMsg msg:
                    TryMoveHeldEntityToActiveHand(msg.HandName);
                    break;
            }
        }

        public void AddHand(string handName)
        {
            if (HasHand(handName))
                return;

            var container = ContainerHelpers.CreateContainer<ContainerSlot>(Owner, handName);
            container.OccludesLight = false;
            var handLocation = HandLocation.Left; //TODO: Set this appropriately

            _hands.Add(new SharedHand(handName, true, handLocation, container));

            HandCountChanged();
            Dirty();
        }

        public void RemoveHand(string handName)
        {
            if (!TryGetServerHand(handName, out var hand))
                return;

            RemoveHand(hand);
        }

        private void RemoveHand(SharedHand hand)
        {
            DropHeldEntityToFloor(hand, intentionalDrop: false);
            hand.Container.Shutdown();
            _hands.Remove(hand);

            HandCountChanged();
            Dirty();
        }

        public bool HasHand(string handName)
        {
            foreach (var hand in _hands)
            {
                if (hand.Name == handName)
                    return true;
            }
            return false;
        }

        private SharedHand? GetServerHand(string handName)
        {
            foreach (var hand in _hands)
            {
                if (hand.Name == handName)
                    return hand;
            }
            return null;
        }

        private SharedHand? GetActiveServerHand()
        {
            if (ActiveHand == null)
                return null;

            return GetServerHand(ActiveHand);
        }

        private bool TryGetServerHand(string handName, [NotNullWhen(true)] out SharedHand? foundHand)
        {
            foundHand = GetServerHand(handName);
            return foundHand != null;
        }

        private bool TryGetActiveHand([NotNullWhen(true)] out SharedHand? activeHand)
        {
            activeHand = GetActiveServerHand();
            return activeHand != null;
        }

        #region Held Entities

        public bool TryGetHeldEntity(string handName, [NotNullWhen(true)] out IEntity? heldEntity)
        {
            heldEntity = null;

            if (!TryGetServerHand(handName, out var hand))
                return false;

            heldEntity = hand.HeldEntity;
            return heldEntity != null;
        }

        public bool TryGetActiveHeldEntity([NotNullWhen(true)] out IEntity? heldEntity)
        {
            heldEntity = GetActiveServerHand()?.HeldEntity;
            return heldEntity != null;
        }

        public override bool IsHolding(IEntity entity)
        {
            foreach (var hand in _hands)
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

        private bool TryGetHandHoldingEntity(IEntity entity, [NotNullWhen(true)] out SharedHand? handFound)
        {
            handFound = null;

            foreach (var hand in _hands)
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
            if (!TryGetServerHand(handName, out var hand))
                return false;

            if (!CanRemoveHeldEntityFromHand(hand))
                return false;

            if (checkActionBlocker && !PlayerCanDrop())
                return false;

            return true;
        }

        public bool Drop(string handName, EntityCoordinates targetDropLocation, bool doMobChecks = true, bool intentional = true)
        {
            if (!TryGetServerHand(handName, out var hand))
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
            if (!TryGetServerHand(handName, out var hand))
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
            if (!TryGetServerHand(handName, out var hand))
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

        private bool CanRemoveHeldEntityFromHand(SharedHand hand)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return false;

            if (!hand.Container.CanRemove(heldEntity))
                return false;

            return true;
        }

        private bool PlayerCanDrop()
        {
            if (!ActionBlockerSystem.CanDrop(Owner))
                return false;

            return true;
        }

        private void RemoveHeldEntityFromHand(SharedHand hand)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return;

            if (!hand.Container.Remove(heldEntity))
            {
                Logger.Error($"{nameof(HandsComponent)} on {Owner} could not remove {heldEntity} from {hand.Container}.");
                return;
            }
            if (heldEntity.TryGetComponent(out ItemComponent? item))
            {
                item.RemovedFromSlot();
                _entitySystemManager.GetEntitySystem<InteractionSystem>().UnequippedHandInteraction(Owner, heldEntity, hand.ToHandState());
            }
            if (heldEntity.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.RenderOrder = heldEntity.EntityManager.CurrentTick.Value;
            }
        }

        private void DropHeldEntity(SharedHand hand, EntityCoordinates targetDropLocation, bool intentionalDrop)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return;

            RemoveHeldEntityFromHand(hand);

            _entitySystemManager.GetEntitySystem<InteractionSystem>().DroppedInteraction(Owner, heldEntity, intentionalDrop);

            heldEntity.Transform.Coordinates = GetFinalDropCoordinates(targetDropLocation);

            OnItemChanged?.Invoke();
            Dirty();
        }

        /// <summary>
        ///     Calculates the final location a dropped item will end up at, accounting for max drop range and collision along the targeted drop path.
        /// </summary>
        private EntityCoordinates GetFinalDropCoordinates(EntityCoordinates targetCoords) //TODO: Clean up this method
        {
            var mapPos = Owner.Transform.MapPosition;
            var targetPos = targetCoords.ToMapPos(Owner.EntityManager);
            var dropDir = targetPos - mapPos.Position;
            var targetVector = Vector2.Zero;

            if (dropDir != Vector2.Zero)
            {
                var targetLength = MathF.Min(dropDir.Length, SharedInteractionSystem.InteractionRange - 0.001f); // InteractionRange is reduced due to InRange not dealing with floating point error
                var newCoords = targetCoords.WithPosition(dropDir.Normalized * targetLength + mapPos.Position).ToMap(Owner.EntityManager);
                var rayLength = EntitySystem.Get<SharedInteractionSystem>().UnobstructedDistance(mapPos, newCoords, ignoredEnt: Owner);
                targetVector = dropDir.Normalized * rayLength;
            }
            var dropCoords = targetCoords.WithPosition(mapPos.Position + targetVector);
            return dropCoords;
        }

        private bool TryDropHeldEntity(SharedHand hand, EntityCoordinates location, bool checkActionBlocker, bool intentionalDrop)
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
        private void DropHeldEntityToFloor(SharedHand hand, bool intentionalDrop)
        {
            DropHeldEntity(hand, Owner.Transform.Coordinates, intentionalDrop);
        }

        private bool CanPutHeldEntityIntoContainer(SharedHand hand, IContainer targetContainer, bool checkActionBlocker)
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

        private void PutHeldEntityIntoContainer(SharedHand hand, IContainer targetContainer)
        {
            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return;

            RemoveHeldEntityFromHand(hand);

            if (!targetContainer.Insert(heldEntity))
            {
                Logger.Error($"{nameof(HandsComponent)} on {Owner} could not insert {heldEntity} into {targetContainer}.");
                return;
            }
            Dirty();
        }

        #endregion

        #region Pickup

        public bool CanPickupEntity(string handName, IEntity entity, bool checkActionBlocker = true)
        {
            if (!TryGetServerHand(handName, out var hand))
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
            if (!TryGetServerHand(handName, out var hand))
                return false;

            return TryPickupEntity(hand, entity, checkActionBlocker);
        }

        public bool TryPickupEntityToActiveHand(IEntity entity, bool checkActionBlocker = true)
        {
            if (!TryGetActiveHand(out var hand))
                return false;

            return TryPickupEntity(hand, entity, checkActionBlocker);
        }

        private bool CanInsertEntityIntoHand(SharedHand hand, IEntity entity)
        {
            if (!hand.Container.CanInsert(entity))
                return false;

            return true;
        }

        private bool PlayerCanPickup()
        {
            if (!ActionBlockerSystem.CanPickup(Owner))
                return false;

            return true;
        }

        private void PutEntityIntoHand(SharedHand hand, IEntity entity)
        {
            if (!hand.Container.Insert(entity))
            {
                Logger.Error($"{nameof(HandsComponent)} on {Owner} could not insert {entity} into {hand.Container}.");
                return;
            }

            _entitySystemManager.GetEntitySystem<InteractionSystem>().EquippedHandInteraction(Owner, entity, hand.ToHandState());
            _entitySystemManager.GetEntitySystem<InteractionSystem>().HandSelectedInteraction(Owner, entity);
            entity.Transform.LocalPosition = Vector2.Zero;

            OnItemChanged?.Invoke();
            Dirty();

            var entityPosition = entity.TryGetContainer(out var container) ? container.Owner.Transform.Coordinates : entity.Transform.Coordinates;

            if (entityPosition != Owner.Transform.Coordinates)
            {
                SendNetworkMessage(new AnimatePickupEntityMessage(entity.Uid, entityPosition));
            }
        }

        private bool TryPickupEntity(SharedHand hand, IEntity entity, bool checkActionBlocker = true)
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
        public void SwapHands() //TODO: Clean up
        {
            if (ActiveHand == null)
                return;

            if (!TryGetActiveHand(out var hand))
                return;

            var index = _hands.IndexOf(hand);
            index++;
            if (index == _hands.Count)
            {
                index = 0;
            }

            ActiveHand = _hands[index].Name;
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

            await _entitySystemManager.GetEntitySystem<InteractionSystem>()
                .Interaction(Owner, activeHeldEntity, heldEntity, EntityCoordinates.Invalid);
        }

        public void UseActiveHeldEntity()
        {
            if (!TryGetActiveHeldEntity(out var heldItem))
                return;

            _entitySystemManager.GetEntitySystem<InteractionSystem>()
                .TryUseInteraction(Owner, heldItem);
        }

        public void ActivateHeldEntity(string handName)
        {
            if (!TryGetHeldEntity(handName, out var heldEntity))
                return;

            _entitySystemManager.GetEntitySystem<InteractionSystem>()
                .TryInteractionActivate(Owner, heldEntity);
        }

        /// <summary>
        ///     Moves an entity from one hand to the active hand.
        /// </summary>
        public bool TryMoveHeldEntityToActiveHand(string handName, bool checkActionBlocker = true)
        {
            if (!TryGetServerHand(handName, out var hand))
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

        private void HandCountChanged()
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new HandCountChangedEvent(Owner));
        }

        /// <summary>
        ///     Tries to pick up an entity into the active hand. If it cannot, tries to pick up the entity into every other hand.
        /// </summary>
        public bool TryPutInActiveHandOrAny(IEntity entity, bool checkActionBlocker = true)
        {
            return TryPutInAnyHand(entity, GetActiveServerHand(), checkActionBlocker);
        }

        /// <summary>
        ///     Tries to pick up an entity into the priority hand, if provided. If it cannot, tries to pick up the entity into every other hand.
        /// </summary>
        public bool TryPutInAnyHand(IEntity entity, string? priorityHandName = null, bool checkActionBlocker = true)
        {
            SharedHand? priorityHand = null;

            if (priorityHandName != null)
                priorityHand = GetServerHand(priorityHandName);

            return TryPutInAnyHand(entity, priorityHand, checkActionBlocker);
        }

        /// <summary>
        ///     Tries to pick up an entity into the priority hand, if provided. Then, tries to pick up the entity into every other hand.
        /// </summary>
        private bool TryPutInAnyHand(IEntity entity, SharedHand? priorityHand = null, bool checkActionBlocker = true)
        {
            if (priorityHand != null)
            {
                if (TryPickupEntity(priorityHand, entity, checkActionBlocker))
                    return true;
            }

            foreach (var hand in _hands)
            {
                if (TryPickupEntity(hand, entity, checkActionBlocker))
                    return true;
            }
            return false;
        }

        #region Pull/Disarm

        void IBodyPartAdded.BodyPartAdded(BodyPartAddedEventArgs args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
                return;

            AddHand(args.Slot);
        }

        void IBodyPartRemoved.BodyPartRemoved(BodyPartRemovedEventArgs args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
                return;

            RemoveHand(args.Slot);
        }

        bool IDisarmedAct.Disarmed(DisarmedActEventArgs eventArgs)
        {
            if (BreakPulls())
                return false;

            var source = eventArgs.Source;
            var target = eventArgs.Target;

            if (source != null)
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/thudswoosh.ogg", source,
                    AudioHelpers.WithVariation(0.025f));

                if (target != null)
                {
                    if (ActiveHand != null && Drop(ActiveHand, false))
                    {
                        source.PopupMessageOtherClients(Loc.GetString("{0} disarms {1}!", source.Name, target.Name));
                        source.PopupMessageCursor(Loc.GetString("You disarm {0}!", target.Name));
                    }
                    else
                    {
                        source.PopupMessageOtherClients(Loc.GetString("{0} shoves {1}!", source.Name, target.Name));
                        source.PopupMessageCursor(Loc.GetString("You shove {0}!", target.Name));
                    }
                }
            }

            return true;
        }

        // We want this to be the last disarm act to run.
        int IDisarmedAct.Priority => int.MaxValue;

        private bool BreakPulls()
        {
            // What is this API??
            if (!Owner.TryGetComponent(out SharedPullerComponent? puller)
                || puller.Pulling == null || !puller.Pulling.TryGetComponent(out PullableComponent? pullable))
                return false;

            return pullable.TryStopPull();
        }

        private void AttemptPull(PullAttemptMessage msg)
        {
            if (!_hands.Any(hand => hand.Enabled))
            {
                msg.Cancelled = true;
            }
        }

        private void StartPulling()
        {
            var firstFreeHand = _hands.FirstOrDefault(hand => hand.Enabled);

            if (firstFreeHand == null)
                return;

            firstFreeHand.Enabled = false;
        }

        private void StopPulling()
        {
            var firstOccupiedHand = _hands.FirstOrDefault(hand => !hand.Enabled);

            if (firstOccupiedHand == null)
                return;

            firstOccupiedHand.Enabled = true;
        }

        #endregion

        #region Old public methods

        public IEnumerable<string> Hands => _hands.Select(h => h.Name);

        public int Count => _hands.Count;

        /// <summary>
        ///     Returns a list of all hand names, with the active hand being first.
        /// </summary>
        public IEnumerable<string> ActivePriorityEnumerable()
        {
            if (ActiveHand != null)
                yield return ActiveHand;

            foreach (var hand in _hands)
            {
                if (hand.Name == ActiveHand || !hand.Enabled)
                    continue;

                yield return hand.Name;
            }
        }

        /// <summary>
        ///     Attempts to use the active held item.
        /// </summary>
        public void ActivateItem()
        {
            UseActiveHeldEntity();
        }

        /// <summary>
        ///     Tries to drop the contents of a hand directly under the player.
        /// </summary>
        public bool Drop(string handName, bool checkActionBlocker = true, bool intentionalDrop = true)
        {
            return TryDropHandToFloor(handName, checkActionBlocker, intentionalDrop);
        }

        /// <summary>
        ///     Tries to drop an entity in a hand directly under the player.
        /// </summary>
        public bool Drop(IEntity entity, bool checkActionBlocker = true, bool intentionalDrop = true)
        {
            return TryDropEntityToFloor(entity, checkActionBlocker, intentionalDrop);
        }

        /// <summary>
        ///     Tries to unequip contents of a hand directly into a container.
        /// </summary>
        public bool Drop(IEntity entity, BaseContainer targetContainer, bool checkActionBlocker = true)
        {
            return TryPutEntityIntoContainer(entity, targetContainer, checkActionBlocker);
        }

        /// <summary>
        ///     Tries to get the ItemComponent on the entity held by a hand.
        /// </summary>
        public ItemComponent? GetItem(string handName)
        {
            if (!TryGetHeldEntity(handName, out var heldEntity))
                return null;

            heldEntity.TryGetComponent(out ItemComponent? item);
            return item;
        }

        /// <summary>
        ///     Tries to get the ItemComponent on the entity held by a hand.
        /// </summary>
        public bool TryGetItem(string handName, [NotNullWhen(true)] out ItemComponent? item)
        {
            item = null;

            if (!TryGetHeldEntity(handName, out var heldEntity))
                return false;

            return heldEntity.TryGetComponent(out item);
        }

        /// <summary>
        ///     Tries to get the ItemComponent off the entity in the active hand.
        /// </summary>
        public ItemComponent? GetActiveHand
        {
            get
            {
                if (!TryGetActiveHeldEntity(out var heldEntity))
                    return null;

                heldEntity.TryGetComponent(out ItemComponent? item);
                return item;
            }
        }

        public IEnumerable<ItemComponent> GetAllHeldItems()
        {
            foreach (var entity in GetAllHeldEntities())
            {
                if (entity.TryGetComponent(out ItemComponent? item))
                    yield return item;
            }
        }

        /// <summary>
        ///     Checks if any hand can pick up an item.
        /// </summary>
        public bool CanPutInHand(ItemComponent item, bool mobCheck = true)
        {
            var entity = item.Owner;

            if (mobCheck && !PlayerCanPickup())
                return false;

            foreach (var hand in _hands)
            {
                if (CanInsertEntityIntoHand(hand, entity))
                    return true;
            }
            return false;
        }

        /// <summary>
        ///     Attempts to put an item into the active hand, or any other hand if it cannot.
        /// </summary>
        public bool PutInHand(ItemComponent item, bool checkActionBlocker = true)
        {
            return TryPutInActiveHandOrAny(item.Owner, checkActionBlocker);
        }

        /// <summary>
        ///     Puts an item any hand, prefering the active hand, or puts it on the floor under the player.
        /// </summary>
        public void PutInHandOrDrop(ItemComponent item, bool checkActionBlocker = true)
        {
            var entity = item.Owner;

            if (!TryPutInActiveHandOrAny(entity, checkActionBlocker))
                entity.Transform.Coordinates = Owner.Transform.Coordinates;
        }

        #endregion
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
