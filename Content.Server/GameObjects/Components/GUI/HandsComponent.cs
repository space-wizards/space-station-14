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
using Robust.Shared.Audio;
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
using Robust.Shared.Player;

namespace Content.Server.GameObjects.Components.GUI
{
    [RegisterComponent]
    [ComponentReference(typeof(IHandsComponent))]
    [ComponentReference(typeof(ISharedHandsComponent))]
    [ComponentReference(typeof(SharedHandsComponent))]
    public class HandsComponent : SharedHandsComponent, IHandsComponent, IBodyPartAdded, IBodyPartRemoved, IDisarmedAct
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        private string? _activeHand;
        private uint _nextHand;

        public event Action? OnItemChanged;

        [ViewVariables(VVAccess.ReadWrite)]
        public string? ActiveHand
        {
            get => _activeHand;
            set
            {
                if (value != null && GetHand(value) == null)
                {
                    throw new ArgumentException($"No hand '{value}'");
                }

                _activeHand = value;
                Dirty();
            }
        }

        [ViewVariables] private readonly List<Hand> _hands = new();

        public IEnumerable<string> Hands => _hands.Select(h => h.Name);

        // Mostly arbitrary.
        public const float PickupRange = 2;

        [ViewVariables] public int Count => _hands.Count;

        // TODO: This does not serialize what objects are held.
        protected override void Startup()
        {
            base.Startup();
            ActiveHand = _hands.LastOrDefault()?.Name;
        }

        public IEnumerable<ItemComponent> GetAllHeldItems()
        {
            foreach (var hand in _hands)
            {
                if (hand.Entity != null)
                {
                    yield return hand.Entity.GetComponent<ItemComponent>();
                }
            }
        }

        public override bool IsHolding(IEntity entity)
        {
            foreach (var hand in _hands)
            {
                if (hand.Entity == entity)
                {
                    return true;
                }
            }
            return false;
        }

        private Hand? GetHand(string? name)
        {
            return _hands.FirstOrDefault(hand => hand.Name == name);
        }

        public ItemComponent? GetItem(string? handName)
        {
            return GetHand(handName)?.Entity?.GetComponent<ItemComponent>();
        }

        public bool TryGetItem(string handName, [NotNullWhen(true)] out ItemComponent? item)
        {
            return (item = GetItem(handName)) != null;
        }

        public ItemComponent? GetActiveHand => ActiveHand == null
            ? null
            : GetItem(ActiveHand);

        /// <summary>
        ///     Enumerates over the enabled hand keys,
        ///     returning the active hand first.
        /// </summary>
        public IEnumerable<string> ActivePriorityEnumerable()
        {
            if (ActiveHand != null)
            {
                yield return ActiveHand;
            }

            foreach (var hand in _hands)
            {
                if (hand.Name == ActiveHand)
                {
                    continue;
                }

                if (!hand.Enabled)
                {
                    continue;
                }

                yield return hand.Name;
            }
        }

        public bool PutInHand(ItemComponent item, bool mobCheck = true)
        {
            foreach (var hand in ActivePriorityEnumerable())
            {
                if (PutInHand(item, hand, false, mobCheck))
                {
                    OnItemChanged?.Invoke();

                    return true;
                }
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

        /// <summary>
        ///     Drops the item if <paramref name="mob"/> doesn't have hands.
        /// </summary>
        public static void PutInHandOrDropStatic(IEntity mob, ItemComponent item, bool mobCheck = true)
        {
            if (!mob.TryGetComponent(out HandsComponent? hands))
            {
                DropAtFeet(mob, item);
                return;
            }

            hands.PutInHandOrDrop(item, mobCheck);
        }

        public void PutInHandOrDrop(ItemComponent item, bool mobCheck = true)
        {
            if (!PutInHand(item, mobCheck))
            {
                DropAtFeet(Owner, item);
            }
        }

        private static void DropAtFeet(IEntity mob, ItemComponent item)
        {
            item.Owner.Transform.Coordinates = mob.Transform.Coordinates;
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

        /// <summary>
        /// Calls the Dropped Interaction with the item.
        /// </summary>
        /// <param name="item">The itemcomponent of the item to be dropped</param>
        /// <param name="doMobChecks">Check if the item can be dropped</param>
        /// <param name="intentional">If the item was dropped intentionally</param>
        /// <returns>True if IDropped.Dropped was called, otherwise false</returns>
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

        public bool Drop(string slot, EntityCoordinates coords, bool doMobChecks = true, bool doDropInteraction = true, bool intentional = true)
        {
            var hand = GetHand(slot);
            if (!CanDrop(slot, doMobChecks) || hand?.Entity == null)
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
            if (coords.EntityId == EntityUid.Invalid)
                item.Owner.Transform.Coordinates = Owner.Transform.Coordinates;
            else
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
            if (slot == null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            if (targetContainer == null)
            {
                throw new ArgumentNullException(nameof(targetContainer));
            }

            var hand = GetHand(slot);
            if (!CanDrop(slot, doMobChecks) || hand?.Entity == null)
            {
                return false;
            }

            if (!hand.Container.CanRemove(hand.Entity))
            {
                return false;
            }

            if (!targetContainer.CanInsert(hand.Entity))
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
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

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

        /// <summary>
        ///     Checks whether an item can be dropped from the specified slot.
        /// </summary>
        /// <param name="name">The slot to check for.</param>
        /// <returns>
        ///     True if there is an item in the slot and it can be dropped, false otherwise.
        /// </returns>
        public bool CanDrop(string name, bool mobCheck = true)
        {
            var hand = GetHand(name);

            if (mobCheck && !ActionBlockerSystem.CanDrop(Owner))
                return false;

            if (hand?.Entity == null)
                return false;

            return hand.Container.CanRemove(hand.Entity);
        }

        public void AddHand(string name)
        {
            if (HasHand(name))
            {
                throw new InvalidOperationException($"Hand '{name}' already exists.");
            }

            var container = ContainerHelpers.CreateContainer<ContainerSlot>(Owner, $"hand {_nextHand++}");
            container.OccludesLight = false;
            var hand = new Hand(this, name, container);

            _hands.Add(hand);

            ActiveHand ??= name;

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
            hand!.Dispose();
            _hands.Remove(hand);

            if (name == ActiveHand)
            {
                _activeHand = _hands.FirstOrDefault()?.Name;
            }

            OnItemChanged?.Invoke();
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new HandCountChangedEvent(Owner));

            Dirty();
        }

        public bool HasHand(string name)
        {
            return _hands.Any(hand => hand.Name == name);
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            var hands = new SharedHand[_hands.Count];

            for (var i = 0; i < _hands.Count; i++)
            {
                var hand = _hands[i].ToShared(i, IndexToHandLocation(i));
                hands[i] = hand;
            }

            return new HandsComponentState(hands, ActiveHand);
        }

        private HandLocation IndexToHandLocation(int index)
        {
            return index == 0
                ? HandLocation.Right
                : index == _hands.Count - 1
                    ? HandLocation.Left
                    : HandLocation.Middle;
        }

        private SharedHand ToSharedHand(Hand hand)
        {
            var index = _hands.IndexOf(hand);
            return hand.ToShared(index, IndexToHandLocation(index));
        }

        public void SwapHands()
        {
            if (ActiveHand == null)
            {
                return;
            }

            var hand = GetHand(ActiveHand);
            if (hand == null)
            {
                throw new InvalidOperationException($"No hand found with name {ActiveHand}");
            }

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

        public bool ThrowItem()
        {
            var item = GetActiveHand?.Owner;
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
            {
                throw new ArgumentNullException(nameof(session));
            }

            switch (message)
            {
                case ClientChangedHandMsg msg:
                {
                    var playerEntity = session.AttachedEntity;

                    if (playerEntity == Owner && HasHand(msg.Index))
                    {
                        ActiveHand = msg.Index;
                    }

                    break;
                }

                case ClientAttackByInHandMsg msg:
                {
                    var hand = GetHand(msg.Index);
                    if (hand == null)
                    {
                        Logger.WarningS("go.comp.hands", "Got a ClientAttackByInHandMsg with invalid hand name '{0}'",
                            msg.Index);
                        return;
                    }

                    var playerEntity = session.AttachedEntity;
                    var used = GetActiveHand?.Owner;

                    if (playerEntity == Owner && hand.Entity != null)
                    {
                        var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                        if (used != null)
                        {
                                await interactionSystem.Interaction(Owner, used, hand.Entity,
                                    EntityCoordinates.Invalid);
                        }
                        else
                        {
                            var entity = hand.Entity;
                            if (!Drop(entity))
                            {
                                break;
                            }

                            interactionSystem.Interaction(Owner, entity);
                        }
                    }

                    break;
                }

                case UseInHandMsg _:
                {
                    var playerEntity = session.AttachedEntity;
                    var used = GetActiveHand?.Owner;

                    if (playerEntity == Owner && used != null)
                    {
                        var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                        interactionSystem.TryUseInteraction(Owner, used);
                    }

                    break;
                }

                case ActivateInHandMsg msg:
                {
                    var playerEntity = session.AttachedEntity;
                    var used = GetItem(msg.Index)?.Owner;

                    if (playerEntity == Owner && used != null)
                    {
                        var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                        interactionSystem.TryInteractionActivate(Owner, used);
                    }
                    break;
                }
            }
        }

        public void HandleSlotModifiedMaybe(ContainerModifiedMessage message)
        {
            foreach (var hand in _hands)
            {
                if (hand.Container != message.Container)
                {
                    continue;
                }

                Dirty();

                if (!message.Entity.TryGetComponent(out IPhysBody? physics))
                {
                    return;
                }

                // set velocity to zero
                physics.LinearVelocity = Vector2.Zero;
                return;
            }
        }

        void IBodyPartAdded.BodyPartAdded(BodyPartAddedEventArgs args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
            {
                return;
            }

            AddHand(args.Slot);
        }

        void IBodyPartRemoved.BodyPartRemoved(BodyPartRemovedEventArgs args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
            {
                return;
            }

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
                SoundSystem.Play(Filter.Pvs(source), "/Audio/Effects/thudswoosh.ogg", source,
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
    }

    public class Hand : IDisposable
    {
        private bool _enabled = true;

        public Hand(HandsComponent parent, string name, ContainerSlot container)
        {
            Parent = parent;
            Name = name;
            Container = container;
        }

        private HandsComponent Parent { get; }
        public string Name { get; }
        public IEntity? Entity => Container.ContainedEntity;
        public ContainerSlot Container { get; }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;
                Parent.Dirty();

                var message = value
                    ? (ComponentMessage) new HandEnabledMsg(Name)
                    : new HandDisabledMsg(Name);

                Parent.HandleMessage(message, Parent);
                Parent.Owner.SendMessage(Parent, message);
            }
        }

        public void Dispose()
        {
            Container.Shutdown(); // TODO verify this
        }

        public SharedHand ToShared(int index, HandLocation location)
        {
            return new(index, Name, Entity?.Uid, location, Enabled);
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
