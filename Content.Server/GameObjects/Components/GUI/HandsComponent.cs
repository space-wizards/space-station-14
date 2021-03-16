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
        public string? ActiveHandName
        {
            get => _activeHandName;
            set
            {
                if (value != null && !HasHand(value))
                {
                    Logger.Warning($"{nameof(HandsComponent)} on {Owner} tried to set its active hand to {value}, which was not a hand.");
                    return;
                }
                _activeHandName = value;
                Dirty();
            }
        }
        private string? _activeHandName;

        [ViewVariables]
        public IReadOnlyList<IReadOnlyHand> ReadOnlyHands => _hands;
        private readonly List<ServerHand> _hands = new();

        protected override void Startup()
        {
            base.Startup();
            ActiveHandName = _hands.FirstOrDefault()?.Name;
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
            return new HandsComponentState(hands, ActiveHandName);
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
            if (ActiveHandName == null)
                return null;

            return GetServerHand(ActiveHandName);
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

        private IEntity? GetHeldEntity(string handName)
        {
            return GetServerHand(handName)?.HeldEntity;
        }

        private IEntity? GetActiveHeldEntity()
        {
            return GetActiveServerHand()?.HeldEntity;
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

        private void DropHeldEntity(ServerHand hand, EntityCoordinates targetCoords, bool intentionalDrop)
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
                _entitySystemManager.GetEntitySystem<InteractionSystem>().UnequippedHandInteraction(Owner, item.Owner, hand.ToHandState());
            }
            if (heldEntity.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.RenderOrder = heldEntity.EntityManager.CurrentTick.Value;
            }

            heldEntity.Transform.Coordinates = GetFinalDropCoordinates(targetCoords);
            _entitySystemManager.GetEntitySystem<InteractionSystem>().DroppedInteraction(Owner, heldEntity, intentionalDrop);

            OnItemChanged?.Invoke();
            Dirty();
        }

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

        private void DropHeldEntityToFloor(ServerHand hand, bool intentionalDrop)
        {
            DropHeldEntity(hand, Owner.Transform.Coordinates, intentionalDrop);
        }

        private bool CanDropHeldEntity(ServerHand hand, bool checkActionBlocker)
        {
            if (checkActionBlocker && !ActionBlockerSystem.CanDrop(Owner))
                return false;

            var heldEntity = hand.HeldEntity;

            if (heldEntity == null || !hand.Container.CanRemove(heldEntity))
                return false;

            return true;
        }

        private bool TryDropHeldEntity(ServerHand hand, EntityCoordinates location, bool checkActionBlocker, bool intentionalDrop)
        {
            if (CanDropHeldEntity(hand, checkActionBlocker))
            {
                DropHeldEntity(hand, location, intentionalDrop);
                return true;
            }
            return false;
        }

        #region Hiding Old Hand Methods

        [ViewVariables] public IEnumerable<string> Hands => _hands.Select(h => h.Name);

        [ViewVariables] public int Count => _hands.Count;

        /// <summary>
        ///     Checks if any hand is holding the provided entity.
        /// </summary>
        public override bool IsHolding(IEntity entity)
        {
            foreach (var hand in _hands)
            {
                if (hand.HeldEntity == entity)
                    return true;
            }
            return false;
        }

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
        ///     Checks if any hand is holding a provided entity, and gets the name of that hand.
        /// </summary>
        public bool TryHand(IEntity entity, [NotNullWhen(true)] out string? handName)
        {
            handName = null;

            foreach (var hand in _hands)
            {
                if (hand.Entity == entity)
                {
                    handName = hand.Name;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Drops the contents of a specific hand.
        /// </summary>
        public bool DropFromHand(string handName, EntityCoordinates coords, bool doMobChecks = true, bool doDropInteraction = true, bool intentional = true)
        {
            if (!TryGetServerHand(handName, out var hand))
                return false;

            return TryDropHeldEntity(hand, coords, doMobChecks, intentional);
        }


        public bool Drop(string slot, BaseContainer targetContainer, bool doMobChecks = true, bool doDropInteraction = true, bool intentional = true)
        {
            if (!TryGetServerHand(slot, out var hand))
                return false;

            if (!CanDrop(slot, doMobChecks) ||
                hand?.Entity == null ||
                !hand.Container.CanRemove(hand.Entity) ||
                !targetContainer.CanInsert(hand.Entity))
            {
                return false;
            }

            var item = hand.Entity.GetComponent<ItemComponent>();

            if (!hand.Container.Remove(hand.Entity))
            {
                throw new InvalidOperationException();
            }

            _entitySystemManager.GetEntitySystem<InteractionSystem>().UnequippedHandInteraction(Owner, item.Owner, hand.ToHandState());

            if (doDropInteraction && !DroppedInteraction(item, doMobChecks, intentional))
                return false;

            item.RemovedFromSlot();

            if (!targetContainer.Insert(item.Owner))
            {
                throw new InvalidOperationException();
            }

            OnItemChanged?.Invoke();

            Dirty();
            return true;
        }

        public bool Drop(IEntity entity, EntityCoordinates coords, bool doMobChecks = true, bool doDropInteraction = true, bool intentional = true)
        {
            if (!TryHand(entity, out var slot))
            {
                throw new ArgumentException("Entity must be held in one of our hands.", nameof(entity));
            }

            return DropFromHand(slot, coords, doMobChecks, doDropInteraction, intentional);
        }

        public bool Drop(string slot, bool mobChecks = true, bool doDropInteraction = true, bool intentional = true)
        {
            return DropFromHand(slot, Owner.Transform.Coordinates, mobChecks, doDropInteraction, intentional);
        }

        public bool Drop(IEntity entity, bool mobChecks = true, bool doDropInteraction = true, bool intentional = true)
        {
            if (!TryHand(entity, out var slot))
            {
                throw new ArgumentException("Entity must be held in one of our hands.", nameof(entity));
            }

            return DropFromHand(slot, Owner.Transform.Coordinates, mobChecks, doDropInteraction, intentional);
        }

        public bool Drop(IEntity entity, BaseContainer targetContainer, bool doMobChecks = true, bool doDropInteraction = true, bool intentional = true)
        {
            if (!TryHand(entity, out var slot))
            {
                throw new ArgumentException("Entity must be held in one of our hands.", nameof(entity));
            }

            return Drop(slot, targetContainer, doMobChecks, doDropInteraction, intentional);
        }

        public bool CanDrop(string handName, bool mobCheck = true)
        {
            if (!TryGetServerHand(handName, out var hand))
                return false;

            if (mobCheck && !ActionBlockerSystem.CanDrop(Owner))
                return false;

            if (hand?.Entity == null)
                return false;

            return hand.Container.CanRemove(hand.Entity);
        }

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
            var heldEntity = GetHeldEntity(handName);

            if (heldEntity == null)
                return;

            _entitySystemManager.GetEntitySystem<InteractionSystem>()
                .TryInteractionActivate(Owner, heldEntity);
        }

        #endregion

        #region Item Stuff, needs shared item

        public ItemComponent? GetItem(string handName) //Old api
        {
            if (!TryGetServerHand(handName, out var hand))
                return null;

            var heldEntity = hand.HeldEntity;
            if (heldEntity == null)
                return null;

            return heldEntity.GetComponent<ItemComponent>();
        }

        public bool TryGetItem(string handName, [NotNullWhen(true)] out ItemComponent? item)
        {
            return (item = GetItem(handName)) != null;
        }

        public string? ActiveHand { get => ActiveHandName; set => ActiveHandName = value; }

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
                yield return heldEntity.GetComponent<ItemComponent>();
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

        private bool DroppedInteraction(ItemComponent item, bool doMobChecks, bool intentional)
        {
            var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
            if (doMobChecks)
            {
                if (!interactionSystem.TryDroppedInteraction(Owner, item.Owner, intentional))
                    return false;
            }
            else
            {
                interactionSystem.DroppedInteraction(Owner, item.Owner, intentional);
            }
            return true;
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

            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/thudswoosh.ogg", source,
                AudioHelpers.WithVariation(0.025f));

            if (ActiveHand != null && Drop(ActiveHand, false))
            {
                source.PopupMessageOtherClients(Loc.GetString("{0} disarms {1}!", source.Name, eventArgs.Target.Name));
                source.PopupMessageCursor(Loc.GetString("You disarm {0}!", eventArgs.Target.Name));
            }
            else
            {
                source.PopupMessageOtherClients(Loc.GetString("{0} shoves {1}!", source.Name, eventArgs.Target.Name));
                source.PopupMessageCursor(Loc.GetString("You shove {0}!", eventArgs.Target.Name));
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
