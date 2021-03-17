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
using Robust.Shared.Physics;
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
        private readonly List<ServerHand> _hands = new();

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

            var used = GetActiveHand?.Owner;

            switch (message)
            {
                case ClientChangedHandMsg msg:
                    TrySetActiveHand(msg.HandName);
                    break;
                case ClientAttackByInHandMsg msg:
                    ClientAttackByInHand(msg.HandName, used);
                    break;
                case UseInHandMsg:
                    UseHeldEntity(used);
                    break;
                case ActivateInHandMsg msg:
                    ActivateHeldEntity(msg.HandName);
                    break;
            }
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

        public bool HasHand(string handName)
        {
            foreach (var hand in _hands)
            {
                if (hand.Name == handName)
                    return true;
            }
            return false;
        }

        public void AddHand(string handName)
        {
            if (HasHand(handName))
                return;

            var container = ContainerHelpers.CreateContainer<ContainerSlot>(Owner, handName);
            container.OccludesLight = false;
            var handLocation = HandLocation.Left; //TODO: Set this appropriately

            _hands.Add(new ServerHand(handName, container, true, handLocation));

            HandCountChanged();
            Dirty();
        }

        public void RemoveHand(string handName)
        {
            if (!TryGetServerHand(handName, out var hand))
                return;

            RemoveHand(hand);
        }

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

        public bool TryPutHeldEntityIntoContainer(string handName, BaseContainer targetContainer, bool doMobChecks = true, bool intentional = true)
        {
            if (!TryGetServerHand(handName, out var hand))
                return false;

            if (!CanPutHeldEntityIntoContainer(hand, targetContainer, doMobChecks))
                return false;

            PutHeldEntityIntoContainer(hand, targetContainer);
            return true;
        }

        /// <summary>
        ///     Tries to unequip contents of a hand directly into a container.
        /// </summary>
        public bool Drop(IEntity entity, BaseContainer targetContainer, bool doMobChecks = true) //TODO: Give better name
        {
            if (!TryGetHandHoldingEntity(entity, out var hand))
                return false;

            if (!CanPutHeldEntityIntoContainer(hand, targetContainer, doMobChecks))
                return false;

            PutHeldEntityIntoContainer(hand, targetContainer);
            return true;
        }

        /// <summary>
        ///     Tries to drop the contents of a hand directly under the player.
        /// </summary>
        public bool Drop(string handName, bool checkActionBlocker = true, bool intentionalDrop = true) //TODO: Give better name
        {
            if (!TryGetServerHand(handName, out var hand))
                return false;

            return TryDropHeldEntity(hand, Owner.Transform.Coordinates, checkActionBlocker, intentionalDrop);
        }

        /// <summary>
        ///     Tries to drop an entity in a hand directly under the player.
        /// </summary>
        public bool Drop(IEntity entity, bool mobChecks = true, bool intentional = true) //TODO: Give better name
        {
            if (!TryGetHandHoldingEntity(entity, out var hand))
                return false;

            return TryDropHeldEntity(hand, Owner.Transform.Coordinates, mobChecks, intentional);
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

        #region to clean up next

        /// <summary>
        ///     Moves the active hand to the next hand.
        /// </summary>
        public void SwapHands()
        {
            if (ActiveHand == null)
            {
                return;
            }

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

        public void ActivateItem()
        {
            var used = GetActiveHand?.Owner;
            if (used != null)
            {
                var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                interactionSystem.TryUseInteraction(Owner, used);
            }
        }

        private void TrySetActiveHand(string handName)
        {
            if (HasHand(handName))
                ActiveHand = handName;
        }

        private async void ClientAttackByInHand(string handName, IEntity? used)
        {
            if (!TryGetServerHand(handName, out var hand))
            {
                Logger.Warning($"{nameof(HandsComponent)} on {Owner} got a {nameof(ClientAttackByInHandMsg)} with invalid hand name {handName}");
                return;
            }
            var heldEntity = hand.HeldEntity;
            if (heldEntity != null)
            {
                var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                if (used != null)
                {
                    await interactionSystem.Interaction(Owner, used, heldEntity, EntityCoordinates.Invalid);
                }
                else
                {
                    if (!Drop(heldEntity))
                        return;

                    interactionSystem.Interaction(Owner, heldEntity);
                }
            }
        }

        private void UseHeldEntity(IEntity? entity)
        {
            if (entity != null)
            {
                var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                interactionSystem.TryUseInteraction(Owner, entity);
            }
        }

        private void ActivateHeldEntity(string handName)
        {
            if (!TryGetServerHand(handName, out var hand))
                return;

            var heldEntity = hand.HeldEntity;

            if (heldEntity == null)
                return;

            _entitySystemManager.GetEntitySystem<InteractionSystem>()
                .TryInteractionActivate(Owner, heldEntity);
        }

        #endregion

        #region Internal Methods

        private ServerHand? GetServerHand(string handName)
        {
            foreach (var hand in _hands)
            {
                if (hand.Name == handName)
                    return hand;
            }
            return null;
        }

        private ServerHand? GetActiveServerHand()
        {
            if (ActiveHand == null)
                return null;

            return GetServerHand(ActiveHand);
        }

        private bool TryGetServerHand(string handName, [NotNullWhen(true)] out ServerHand? foundHand)
        {
            foundHand = GetServerHand(handName);
            return foundHand != null;
        }

        private bool TryGetActiveHand([NotNullWhen(true)] out ServerHand? activeHand)
        {
            activeHand = GetActiveServerHand();
            return activeHand != null;
        }

        private bool TryGetHandHoldingEntity(IEntity entity, [NotNullWhen(true)] out ServerHand? handFound)
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

        private void RemoveHand(ServerHand hand)
        {
            DropHeldEntityToFloor(hand, intentionalDrop: false);
            hand.Container.Shutdown();
            _hands.Remove(hand);

            HandCountChanged();
            Dirty();
        }

        private void HandCountChanged()
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new HandCountChangedEvent(Owner));
        }

        private bool CanRemoveHeldEntityFromHand(ServerHand hand)
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

        private void RemoveHeldEntityFromHand(ServerHand hand)
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

        private void DropHeldEntity(ServerHand hand, EntityCoordinates targetDropLocation, bool intentionalDrop)
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

        private bool TryDropHeldEntity(ServerHand hand, EntityCoordinates location, bool checkActionBlocker, bool intentionalDrop)
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
        private void DropHeldEntityToFloor(ServerHand hand, bool intentionalDrop)
        {
            DropHeldEntity(hand, Owner.Transform.Coordinates, intentionalDrop);
        }

        private bool CanPutHeldEntityIntoContainer(ServerHand hand, IContainer targetContainer, bool checkActionBlocker)
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

        private void PutHeldEntityIntoContainer(ServerHand hand, IContainer targetContainer)
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

        #region Old hand methods that I don't want to rename and make a huge diff

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

        #endregion

        #region Old API w/ ItemComponent

        public ItemComponent? GetItem(string handName)
        {
            if (!TryGetServerHand(handName, out var hand))
                return null;

            var heldEntity = hand.HeldEntity;
            if (heldEntity == null)
                return null;

            heldEntity.TryGetComponent(out ItemComponent? item);

            return item;
        }

        public bool TryGetItem(string handName, [NotNullWhen(true)] out ItemComponent? item)
        {
            return (item = GetItem(handName)) != null;
        }

        public ItemComponent? GetActiveHand
        {
            get
            {
                if (ActiveHand == null)
                    return null;

                return GetItem(ActiveHand);
            }
        }

        public IEnumerable<ItemComponent> GetAllHeldItems()
        {
            foreach (var hand in _hands)
            {
                var heldEntity = hand.HeldEntity;
                if (heldEntity == null)
                    continue;

                if (heldEntity.TryGetComponent(out ItemComponent? item))
                    yield return item;
            }
        }

        /// <summary>
        ///     Attempts to put item into a hand, prefering the active hand.
        /// </summary>
        public bool PutInHand(ItemComponent item, bool mobCheck = true)
        {
            foreach (var hand in ActivePriorityEnumerable())
            {
                if (!TryPutItemInHand(item, hand, false, mobCheck))
                    continue;

                OnItemChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool TryPutItemInHand(ItemComponent item, string handName, bool fallback = true, bool mobChecks = true)
        {
            if (!TryGetServerHand(handName, out var hand))
                return false;

            if (!CanPutInHand(item, handName, mobChecks))
            {
                return fallback && PutInHand(item);
            }

            Dirty();

            var position = item.Owner.Transform.Coordinates;
            var contained = item.Owner.IsInContainer();
            var success = hand.Container.Insert(item.Owner);
            if (success)
            {
                //If the entity isn't in a container, and it isn't located exactly at our position (i.e. in our own storage), then we can safely play the animation
                if (position != Owner.Transform.Coordinates && !contained)
                {
                    SendNetworkMessage(new AnimatePickupEntityMessage(item.Owner.Uid, position));
                }
                item.Owner.Transform.LocalPosition = Vector2.Zero;
                OnItemChanged?.Invoke();
            }

            _entitySystemManager.GetEntitySystem<InteractionSystem>().EquippedHandInteraction(Owner, item.Owner, hand.ToHandState());

            _entitySystemManager.GetEntitySystem<InteractionSystem>().HandSelectedInteraction(Owner, item.Owner);

            return success;
        }

        /// <summary>
        ///     Puts an item in a hand, preferring the active hand, or puts it on the floor under the player.
        /// </summary>
        public void PutInHandOrDrop(ItemComponent item, bool mobCheck = true)
        {
            if (!PutInHand(item, mobCheck))
                item.Owner.Transform.Coordinates = Owner.Transform.Coordinates;
        }

        public bool CanPutInHand(ItemComponent item, bool mobCheck = true)
        {
            if (mobCheck && !ActionBlockerSystem.CanPickup(Owner))
                return false;

            foreach (var handName in ActivePriorityEnumerable())
            {
                // We already did a mobCheck, so let's not waste cycles.
                if (CanPutInHand(item, handName, false))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanPutInHand(ItemComponent item, string handName, bool mobCheck = true)
        {
            if (mobCheck && !ActionBlockerSystem.CanPickup(Owner))
                return false;

            if (!TryGetServerHand(handName, out var hand))
                return false;

            return hand.Enabled &&
                   hand.Container.CanInsert(item.Owner);
        }

        #endregion

        #region Hiding misc pull/disarm

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
    }

    public class ServerHand : SharedHand
    {
        public override IEntity? HeldEntity => Container.ContainedEntity;

        public IEntity? Entity => Container.ContainedEntity; //TODO: remove this duplicate API

        public ContainerSlot Container { get; }

        public ServerHand(string name, ContainerSlot container, bool enabled, HandLocation location) : base(name, enabled, location)
        {
            Container = container;
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
