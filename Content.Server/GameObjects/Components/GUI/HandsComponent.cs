#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
using Robust.Shared.Physics;

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
                if (value != null && GetHand(value) == null)
                {
                    throw new ArgumentException($"No hand '{value}'");
                }

                _activeHandName = value;
                Dirty();
            }
        }
        private string? _activeHandName;

        [ViewVariables] private readonly List<ServerHand> _hands = new();

        [ViewVariables]
        public IEnumerable<string> HandNames => _hands.Select(h => h.Name);

        [ViewVariables]
        public int HandCount => _hands.Count;

        // TODO: This does not serialize what objects are held.
        protected override void Startup()
        {
            base.Startup();
            ActiveHandName = _hands.LastOrDefault()?.Name;
            Dirty();
        }

        IEnumerable<ItemComponent> IHandsComponent.GetAllHeldItems()
        {
            foreach (var hand in _hands)
            {
                var heldEntity = hand.HeldEntity;
                if (heldEntity == null)
                    continue;
                yield return heldEntity.GetComponent<ItemComponent>();
            }
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

        private bool TryGetHand(string handName, [NotNullWhen(true)] out ServerHand? foundHand)
        {
            foundHand = null;

            if (handName == null)
                return false;

            foreach (var hand in _hands)
            {
                if (hand.Name == handName)
                    foundHand = hand;
            }
            return foundHand != null;
        }

        private ServerHand? GetHand(string handName) //Old api
        {
            return TryGetHand(handName, out var hand) ? hand : null;
        }

        public ItemComponent? GetItem(string handName) //Old api
        {
            if (!TryGetHand(handName, out var hand))
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

        public ItemComponent? GetActiveHeldItem
        {
            get
            {
                if (ActiveHandName == null)
                    return null;

                return GetItem(ActiveHandName);
            }
        }

        /// <summary>
        ///     Enumerates over the enabled hand keys, returning the active hand first.
        /// </summary>
        public IEnumerable<string> ActivePriorityEnumerable()
        {
            if (ActiveHandName != null)
                yield return ActiveHandName;

            foreach (var hand in _hands)
            {
                if (hand.Name == ActiveHandName || !hand.Enabled)
                    continue;

                yield return hand.Name;
            }
        }

        /// <summary>
        ///     Attempts to put item into a hand, prefering the active hand.
        /// </summary>
        public bool PutInHand(ItemComponent item, bool mobCheck = true)
        {
            foreach (var hand in ActivePriorityEnumerable())
            {
                if (!PutInHand(item, hand, false, mobCheck))
                    continue;

                OnItemChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool PutInHand(ItemComponent item, string index, bool fallback = true, bool mobChecks = true)
        {
            var hand = GetHand(index);
            if (!CanPutInHand(item, index, mobChecks) || hand == null)
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

            _entitySystemManager.GetEntitySystem<InteractionSystem>().EquippedHandInteraction(Owner, item.Owner,
                ToSharedHand(hand));

            _entitySystemManager.GetEntitySystem<InteractionSystem>().HandSelectedInteraction(Owner, item.Owner);

            return success;
        }

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

        public bool CanPutInHand(ItemComponent item, string index, bool mobCheck = true)
        {
            if (mobCheck && !ActionBlockerSystem.CanPickup(Owner))
                return false;

            var hand = GetHand(index);

            return hand != null &&
                   hand.Enabled &&
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

        public bool Drop(string handName, EntityCoordinates coords, bool doMobChecks = true, bool doDropInteraction = true, bool intentional = true)
        {
            var hand = GetHand(handName);
            if (!CanDrop(handName, doMobChecks) || hand?.Entity == null)
            {
                return false;
            }

            var item = hand.Entity.GetComponent<ItemComponent>();

            if (!hand.Container.Remove(hand.Entity))
            {
                return false;
            }

            _entitySystemManager.GetEntitySystem<InteractionSystem>().UnequippedHandInteraction(Owner, item.Owner,
                ToSharedHand(hand));

            if (doDropInteraction && !DroppedInteraction(item, false, intentional))
                return false;

            item.RemovedFromSlot();
            item.Owner.Transform.Coordinates = coords;

            if (item.Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
            {
                spriteComponent.RenderOrder = item.Owner.EntityManager.CurrentTick.Value;
            }

            if (Owner.TryGetContainer(out var container))
            {
                container.Insert(item.Owner);
            }

            OnItemChanged?.Invoke();

            Dirty();
            return true;
        }


        public bool Drop(string slot, BaseContainer targetContainer, bool doMobChecks = true, bool doDropInteraction = true, bool intentional = true)
        {
            if (targetContainer == null)
            {
                throw new ArgumentNullException(nameof(targetContainer));
            }

            var hand = GetHand(slot);
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

            _entitySystemManager.GetEntitySystem<InteractionSystem>().UnequippedHandInteraction(Owner, item.Owner,
                ToSharedHand(hand));

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

            return Drop(slot, coords, doMobChecks, doDropInteraction, intentional);
        }

        public bool Drop(string slot, bool mobChecks = true, bool doDropInteraction = true, bool intentional = true)
        {
            return Drop(slot, Owner.Transform.Coordinates, mobChecks, doDropInteraction, intentional);
        }

        public bool Drop(IEntity entity, bool mobChecks = true, bool doDropInteraction = true, bool intentional = true)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (!TryHand(entity, out var slot))
            {
                throw new ArgumentException("Entity must be held in one of our hands.", nameof(entity));
            }

            return Drop(slot, Owner.Transform.Coordinates, mobChecks, doDropInteraction, intentional);
        }

        public bool Drop(IEntity entity, BaseContainer targetContainer, bool doMobChecks = true, bool doDropInteraction = true, bool intentional = true)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (!TryHand(entity, out var slot))
            {
                throw new ArgumentException("Entity must be held in one of our hands.", nameof(entity));
            }

            return Drop(slot, targetContainer, doMobChecks, doDropInteraction, intentional);
        }

        public bool CanDrop(string name, bool mobCheck = true)
        {
            var hand = GetHand(name);

            if (mobCheck && !ActionBlockerSystem.CanDrop(Owner))
                return false;

            if (hand?.Entity == null)
                return false;

            return hand.Container.CanRemove(hand.Entity);
        }

        public void AddHand(string name, bool enabled = true)
        {
            if (HasHand(name))
            {
                throw new InvalidOperationException($"Hand '{name}' already exists.");
            }

            var container = ContainerHelpers.CreateContainer<ContainerSlot>(Owner, name);
            container.OccludesLight = false;

            var handLocation = HandLocation.Left; //TODO: Set this appropriately

            var hand = new ServerHand(name, container, enabled, handLocation);

            _hands.Add(hand);

            ActiveHandName ??= name;

            OnItemChanged?.Invoke();
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new HandCountChangedEvent(Owner));

            Dirty();
        }

        public void RemoveHand(string name)
        {
            var hand = GetHand(name);
            if (hand == null)
            {
                throw new InvalidOperationException($"Hand '{name}' does not exist.");
            }
            Drop(hand.Name, false);
            _hands.Remove(hand);

            if (name == ActiveHandName)
            {
                _activeHandName = _hands.FirstOrDefault()?.Name;
            }

            OnItemChanged?.Invoke();
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new HandCountChangedEvent(Owner));
            hand.Container.Shutdown();

            Dirty();
        }

        public bool HasHand(string name)
        {
            return _hands.Any(hand => hand.Name == name);
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

        private HandState ToSharedHand(ServerHand hand)
        {
            var index = _hands.IndexOf(hand);
            return hand.ToHandState();
        }

        public void SwapHands()
        {
            if (ActiveHandName == null)
            {
                return;
            }

            var hand = GetHand(ActiveHandName);
            if (hand == null)
            {
                throw new InvalidOperationException($"No hand found with name {ActiveHandName}");
            }

            var index = _hands.IndexOf(hand);
            index++;
            if (index == _hands.Count)
            {
                index = 0;
            }

            ActiveHandName = _hands[index].Name;
        }

        public void ActivateItem()
        {
            var used = GetActiveHeldItem?.Owner;
            if (used != null)
            {
                var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                interactionSystem.TryUseInteraction(Owner, used);
            }
        }

        public bool ThrowItem()
        {
            var item = GetActiveHeldItem?.Owner;
            if (item != null)
            {
                var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                return interactionSystem.TryThrowInteraction(Owner, item);
            }

            return false;
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            if (message is PullMessage pullMessage &&
                pullMessage.Puller.Owner != Owner)
            {
                return;
            }

            switch (message)
            {
                case PullAttemptMessage msg:
                    if (!_hands.Any(hand => hand.Enabled))
                    {
                        msg.Cancelled = true;
                    }

                    break;
                case PullStartedMessage _:
                    var firstFreeHand = _hands.FirstOrDefault(hand => hand.Enabled);

                    if (firstFreeHand == null)
                    {
                        break;
                    }

                    firstFreeHand.Enabled = false;

                    break;
                case PullStoppedMessage _:
                    var firstOccupiedHand = _hands.FirstOrDefault(hand => !hand.Enabled);

                    if (firstOccupiedHand == null)
                    {
                        break;
                    }

                    firstOccupiedHand.Enabled = true;

                    break;
                case HandDisabledMsg msg:
                    Drop(msg.Name, false);
                    break;
            }
        }

        public override async void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var playerEntity = session.AttachedEntity;
            var used = GetActiveHeldItem?.Owner;

            switch (message)
            {
                case ClientChangedHandMsg msg:
                    ClientChangedHandMsg(msg, playerEntity);
                    break;
                case ClientAttackByInHandMsg msg:
                    HandleClientAttackByInHandMsg(msg, playerEntity, used);
                    break;
                case UseInHandMsg:
                    HandleUseInHandMsg(playerEntity, used);
                    break;
                case ActivateInHandMsg msg:
                    HandleActivateInHandMsg(msg, playerEntity, used);
                    break;
            }
        }

        private void ClientChangedHandMsg(ClientChangedHandMsg msg, IEntity? playerEntity)
        {
            if (playerEntity == Owner && HasHand(msg.Index))
                ActiveHandName = msg.Index;
        }

        private async void HandleClientAttackByInHandMsg(ClientAttackByInHandMsg msg, IEntity? playerEntity, IEntity? used)
        {
            var hand = GetHand(msg.Index);
            if (hand == null)
            {
                Logger.WarningS("go.comp.hands", "Got a ClientAttackByInHandMsg with invalid hand name '{0}'", msg.Index);
                return;
            }
            if (playerEntity == Owner && hand.Entity != null)
            {
                var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                if (used != null)
                {
                    await interactionSystem.Interaction(Owner, used, hand.Entity, EntityCoordinates.Invalid);
                }
                else
                {
                    var entity = hand.Entity;
                    if (!Drop(entity))
                        return;

                    interactionSystem.Interaction(Owner, entity);
                }
            }
        }

        private void HandleUseInHandMsg(IEntity? playerEntity, IEntity? used)
        {
            if (playerEntity == Owner && used != null)
            {
                var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                interactionSystem.TryUseInteraction(Owner, used);
            }
        }

        private void HandleActivateInHandMsg(ActivateInHandMsg msg, IEntity? playerEntity, IEntity? used)
        {
            if (playerEntity == Owner && used != null)
            {
                var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                interactionSystem.TryInteractionActivate(Owner, used);
            }
        }

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

        #region Pulling & Disarm Bandaid

        bool IDisarmedAct.Disarmed(DisarmedActEventArgs eventArgs)
        {
            if (BreakPulls())
                return false;

            var source = eventArgs.Source;

            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/thudswoosh.ogg", source,
                AudioHelpers.WithVariation(0.025f));

            if (ActiveHandName != null && Drop(ActiveHandName, false))
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
