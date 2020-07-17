#nullable enable
// Only unused on .NET Core due to KeyValuePair.Deconstruct
// ReSharper disable once RedundantUsingDirective
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Items;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.BodySystem;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.GUI
{
    [RegisterComponent]
    [ComponentReference(typeof(IHandsComponent))]
    public class HandsComponent : SharedHandsComponent, IHandsComponent, IBodyPartAdded, IBodyPartRemoved
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
#pragma warning restore 649

        private string? _activeIndex;

        [ViewVariables(VVAccess.ReadWrite)]
        public string? ActiveIndex
        {
            get => _activeIndex;
            set
            {
                if (this[value] == null)
                {
                    throw new ArgumentException($"No hand '{value}'");
                }

                _activeIndex = value;
                Dirty();
            }
        }

        [ViewVariables] private readonly List<Hand> _hands = new List<Hand>();

        // Mostly arbitrary.
        public const float PickupRange = 2;

        [CanBeNull]
        public Hand this[string? slotName] => _hands.FirstOrDefault(hand => hand.Name == slotName);

        // TODO: This does not serialize what objects are held.
        protected override void Startup()
        {
            base.Startup();
            ActiveIndex = _hands.LastOrDefault()?.Name;
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

        public bool IsHolding(IEntity entity)
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

        public ItemComponent? GetHand(string index)
        {
            return this[index]?.Entity?.GetComponent<ItemComponent>();
        }

        public ItemComponent? GetActiveHand => ActiveIndex == null
            ? null
            : GetHand(ActiveIndex);

        /// <summary>
        ///     Enumerates over the hand keys, returning the active hand first.
        /// </summary>
        public IEnumerable<string> ActivePriorityEnumerable()
        {
            if (ActiveIndex != null)
            {
                yield return ActiveIndex;
            }

            foreach (var hand in _hands)
            {
                if (hand.Name == ActiveIndex)
                {
                    continue;
                }

                yield return hand.Name;
            }
        }

        public bool PutInHand(ItemComponent item)
        {
            foreach (var hand in ActivePriorityEnumerable())
            {
                if (PutInHand(item, hand, false))
                {
                    return true;
                }
            }

            return false;
        }

        public bool PutInHand(ItemComponent item, string index, bool fallback = true)
        {
            var hand = this[index];
            if (!CanPutInHand(item, index) || hand == null)
            {
                return fallback && PutInHand(item);
            }

            Dirty();
            var success = hand.Container.Insert(item.Owner);
            if (success)
            {
                item.Owner.Transform.LocalPosition = Vector2.Zero;
            }

            _entitySystemManager.GetEntitySystem<InteractionSystem>().HandSelectedInteraction(Owner, item.Owner);

            return success;
        }

        public void PutInHandOrDrop(ItemComponent item)
        {
            if (!PutInHand(item))
            {
                item.Owner.Transform.GridPosition = Owner.Transform.GridPosition;
            }
        }

        public bool CanPutInHand(ItemComponent item)
        {
            foreach (var handName in ActivePriorityEnumerable())
            {
                if (CanPutInHand(item, handName))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanPutInHand(ItemComponent item, string index)
        {
            return this[index]?.Container.CanInsert(item.Owner) == true;
        }

        public bool TryHand(IEntity entity, [MaybeNullWhen(false)] out string handName)
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

        public bool Drop(string slot, GridCoordinates coords, bool doMobChecks = true)
        {
            var hand = this[slot];
            if (!CanDrop(slot) || hand?.Entity == null)
            {
                return false;
            }

            var item = hand.Entity.GetComponent<ItemComponent>();

            if (!hand.Container.Remove(hand.Entity))
            {
                return false;
            }

            if (doMobChecks &&
                !_entitySystemManager.GetEntitySystem<InteractionSystem>().TryDroppedInteraction(Owner, item.Owner))
            {
                return false;
            }

            if (ContainerHelpers.TryGetContainer(Owner, out var container) &&
                !container.Insert(item.Owner))
            {
                return false;
            }

            item.RemovedFromSlot();
            item.Owner.Transform.GridPosition = coords;

            Dirty();
            return true;
        }

        public bool Drop(IEntity entity, GridCoordinates coords, bool doMobChecks = true)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (!TryHand(entity, out var slot))
            {
                throw new ArgumentException("Entity must be held in one of our hands.", nameof(entity));
            }

            return Drop(slot, coords, doMobChecks);
        }

        public bool Drop(string slot, bool doMobChecks = true)
        {
            var hand = this[slot];
            if (!CanDrop(slot) || hand?.Entity == null)
            {
                return false;
            }

            var item = hand.Entity.GetComponent<ItemComponent>();

            if (doMobChecks &&
                !_entitySystemManager.GetEntitySystem<InteractionSystem>().TryDroppedInteraction(Owner, item.Owner))
            {
                return false;
            }

            if (!hand.Container.Remove(hand.Entity))
            {
                return false;
            }

            if (ContainerHelpers.TryGetContainer(Owner, out var container) &&
                !container.Insert(item.Owner))
            {
                return false;
            }

            item.RemovedFromSlot();
            item.Owner.Transform.GridPosition = Owner.Transform.GridPosition;

            if (item.Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
            {
                spriteComponent.RenderOrder = item.Owner.EntityManager.CurrentTick.Value;
            }

            Dirty();
            return true;
        }

        public bool Drop(IEntity entity, bool doMobChecks = true)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (!TryHand(entity, out var slot))
            {
                throw new ArgumentException("Entity must be held in one of our hands.", nameof(entity));
            }

            return Drop(slot, doMobChecks);
        }

        public bool Drop(string slot, BaseContainer targetContainer, bool doMobChecks = true)
        {
            if (slot == null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            if (targetContainer == null)
            {
                throw new ArgumentNullException(nameof(targetContainer));
            }

            var hand = this[slot];
            if (!CanDrop(slot) || hand?.Entity == null)
            {
                return false;
            }

            var item = hand.Entity.GetComponent<ItemComponent>();

            if (doMobChecks && !_entitySystemManager.GetEntitySystem<InteractionSystem>().TryDroppedInteraction(Owner, item.Owner))
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

            if (!hand.Container.Remove(hand.Entity))
            {
                throw new InvalidOperationException();
            }

            item.RemovedFromSlot();

            if (!targetContainer.Insert(item.Owner))
            {
                throw new InvalidOperationException();
            }

            Dirty();
            return true;
        }

        public bool Drop(IEntity entity, BaseContainer targetContainer, bool doMobChecks = true)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (!TryHand(entity, out var slot))
            {
                throw new ArgumentException("Entity must be held in one of our hands.", nameof(entity));
            }

            return Drop(slot, targetContainer, doMobChecks);
        }

        /// <summary>
        ///     Checks whether an item can be dropped from the specified slot.
        /// </summary>
        /// <param name="slot">The slot to check for.</param>
        /// <returns>
        ///     True if there is an item in the slot and it can be dropped, false otherwise.
        /// </returns>
        public bool CanDrop(string slot)
        {
            var hand = this[slot];
            if (hand?.Entity == null)
            {
                return false;
            }

            if (ContainerHelpers.TryGetContainer(Owner, out var container) &&
                !container.CanInsert(hand.Entity))
            {
                return false;
            }

            return hand.Container.CanRemove(hand.Entity);
        }

        // TODO: This but better
        private HandLocation GetLocation(string index)
        {
            if (index.Contains("left") && _hands.All(x => x.Location != HandLocation.Left))
            {
                return HandLocation.Left;
            }
            else if (index.Contains("right") && _hands.All(x => x.Location != HandLocation.Right))
            {
                return HandLocation.Right;
            }

            return HandLocation.Middle;
        }

        public void AddHand(string index)
        {
            if (HasHand(index))
            {
                throw new InvalidOperationException($"Hand '{index}' already exists.");
            }

            var container = ContainerManagerComponent.Create<ContainerSlot>(Name + "_" + index, Owner);
            var location = GetLocation(index);
            var hand = new Hand(index, location, container);

            if (_hands.Count == 0 || hand.Location == HandLocation.Left)
            {
                _hands.Add(hand);
            }
            else if (hand.Location == HandLocation.Right)
            {
                _hands.Insert(0, hand);
            }
            else
            {
                _hands.Insert(1, hand);
            }

            ActiveIndex ??= index;

            Dirty();
        }

        public void RemoveHand(string index)
        {
            if (!HasHand(index))
            {
                throw new InvalidOperationException($"Hand '{index}' does not exist.");
            }

            var hand = this[index];
            hand!.Dispose();
            _hands.Remove(hand);

            if (index == ActiveIndex)
            {
                _activeIndex = _hands.FirstOrDefault()?.Name;
            }

            Dirty();
        }

        public bool HasHand(string index)
        {
            return this[index] != null;
        }

        public override ComponentState GetComponentState()
        {
            var hands = _hands.Select(x => x.ToShared()).ToList();
            return new HandsComponentState(hands, ActiveIndex);
        }

        public void SwapHands()
        {
            var hand = this[ActiveIndex];
            if (hand == null)
            {
                throw new InvalidOperationException($"No hand found with index {ActiveIndex}");
            }

            var index = _hands.IndexOf(hand);
            index--;
            if (index < 0)
            {
                index = _hands.Count - 1;
            }

            ActiveIndex = _hands[index].Name;
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

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
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
                        ActiveIndex = msg.Index;
                    }

                    break;
                }

                case ClientAttackByInHandMsg msg:
                {
                    var hand = this[msg.Index];
                    if (hand == null)
                    {
                        Logger.WarningS("go.comp.hands", "Got a ClientAttackByInHandMsg with invalid hand index '{0}'",
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
                            interactionSystem.Interaction(Owner, used, hand.Entity,
                                GridCoordinates.InvalidGrid);
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
                    var used = GetHand(msg.Index)?.Owner;

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

                if (!message.Entity.TryGetComponent(out PhysicsComponent physics))
                {
                    return;
                }

                // set velocity to zero
                physics.LinearVelocity = Vector2.Zero;
                return;
            }
        }

        void IBodyPartAdded.BodyPartAdded(BodyPartAddedEventArgs eventArgs)
        {
            if (eventArgs.Part.PartType != BodyPartType.Hand)
            {
                return;
            }

            AddHand(eventArgs.SlotName);
        }

        void IBodyPartRemoved.BodyPartRemoved(BodyPartRemovedEventArgs eventArgs)
        {
            if (eventArgs.Part.PartType != BodyPartType.Hand)
            {
                return;
            }

            RemoveHand(eventArgs.SlotName);
        }
    }

    public class Hand : IDisposable
    {
        public Hand(string name, HandLocation location, ContainerSlot container)
        {
            Name = name;
            Location = location;
            Container = container;
        }

        public string Name { get; }
        public HandLocation Location { get; }
        public IEntity? Entity => Container.ContainedEntity;
        public ContainerSlot Container { get; }

        public void Dispose()
        {
            Container.Shutdown(); // TODO verify this
        }

        public SharedHand ToShared()
        {
            return new SharedHand(Name, Entity?.Uid, Location);
        }
    }
}
