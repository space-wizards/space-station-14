using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
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
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(IHandsComponent))]
    public class HandsComponent : SharedHandsComponent, IHandsComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        private string _activeIndex;

        [ViewVariables(VVAccess.ReadWrite)]
        public string ActiveIndex
        {
            get => _activeIndex;
            set
            {
                if (!_hands.ContainsKey(value))
                {
                    throw new ArgumentException($"No hand '{value}'");
                }

                _activeIndex = value;
                Dirty();
            }
        }

        [ViewVariables] private readonly Dictionary<string, ContainerSlot> _hands = new Dictionary<string, ContainerSlot>();
        [ViewVariables] private List<string> _orderedHands = new List<string>();

        // Mostly arbitrary.
        public const float PickupRange = 2;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: This does not serialize what objects are held.
            serializer.DataField(ref _orderedHands, "hands", new List<string>(0));
            if (serializer.Reading)
            {
                foreach (var handsname in _orderedHands)
                {
                    AddHand(handsname);
                }
            }

            serializer.DataField(ref _activeIndex, "defaultHand", _orderedHands.LastOrDefault());
        }

        public IEnumerable<ItemComponent> GetAllHeldItems()
        {
            foreach (var slot in _hands.Values)
            {
                if (slot.ContainedEntity != null)
                {
                    yield return slot.ContainedEntity.GetComponent<ItemComponent>();
                }
            }
        }

        public bool IsHolding(IEntity entity)
        {
            foreach (var slot in _hands.Values)
            {
                if (slot.ContainedEntity == entity)
                {
                    return true;
                }
            }
            return false;
        }

        public ItemComponent GetHand(string index)
        {
            var slot = _hands[index];
            return slot.ContainedEntity?.GetComponent<ItemComponent>();
        }

        public ItemComponent GetActiveHand => GetHand(ActiveIndex);

        /// <summary>
        ///     Enumerates over the hand keys, returning the active hand first.
        /// </summary>
        public IEnumerable<string> ActivePriorityEnumerable()
        {
            yield return ActiveIndex;
            foreach (var hand in _hands.Keys)
            {
                if (hand == ActiveIndex)
                {
                    continue;
                }

                yield return hand;
            }
        }

        public bool PutInHand(ItemComponent item)
        {
            foreach (var hand in ActivePriorityEnumerable())
            {
                if (PutInHand(item, hand, fallback: false))
                {
                    return true;
                }
            }

            return false;
        }

        public bool PutInHand(ItemComponent item, string index, bool fallback = true)
        {
            if (!CanPutInHand(item, index))
            {
                return fallback && PutInHand(item);
            }

            var slot = _hands[index];
            Dirty();
            var success = slot.Insert(item.Owner);
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
                item.Owner.Transform.GridPosition = Owner.Transform.GridPosition;
        }

        public bool CanPutInHand(ItemComponent item)
        {
            foreach (var hand in ActivePriorityEnumerable())
            {
                if (CanPutInHand(item, hand))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanPutInHand(ItemComponent item, string index)
        {
            var slot = _hands[index];
            return slot.CanInsert(item.Owner);
        }

        public string FindHand(IEntity entity)
        {
            foreach (var (index, slot) in _hands)
            {
                if (slot.ContainedEntity == entity)
                {
                    return index;
                }
            }

            return null;
        }

        public bool Drop(string slot, GridCoordinates coords, bool doMobChecks = true)
        {
            if (!CanDrop(slot))
            {
                return false;
            }

            var inventorySlot = _hands[slot];
            var item = inventorySlot.ContainedEntity.GetComponent<ItemComponent>();

            if (!inventorySlot.Remove(inventorySlot.ContainedEntity))
            {
                return false;
            }

            if (doMobChecks && !_entitySystemManager.GetEntitySystem<InteractionSystem>().TryDroppedInteraction(Owner, item.Owner))
                return false;

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

            var slot = FindHand(entity);
            if (slot == null)
            {
                throw new ArgumentException("Entity must be held in one of our hands.", nameof(entity));
            }

            return Drop(slot, coords, doMobChecks);
        }

        public bool Drop(string slot, bool doMobChecks = true)
        {
            if (!CanDrop(slot))
            {
                return false;
            }

            var inventorySlot = _hands[slot];
            var item = inventorySlot.ContainedEntity.GetComponent<ItemComponent>();

            if (doMobChecks && !_entitySystemManager.GetEntitySystem<InteractionSystem>().TryDroppedInteraction(Owner, item.Owner))
                return false;

            if (!inventorySlot.Remove(inventorySlot.ContainedEntity))
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

            var slot = FindHand(entity);
            if (slot == null)
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

            if (!CanDrop(slot))
            {
                return false;
            }


            var inventorySlot = _hands[slot];
            var item = inventorySlot.ContainedEntity.GetComponent<ItemComponent>();

            if (doMobChecks && !_entitySystemManager.GetEntitySystem<InteractionSystem>().TryDroppedInteraction(Owner, item.Owner))
            {
                return false;
            }

            if (!inventorySlot.CanRemove(inventorySlot.ContainedEntity))
            {
                return false;
            }

            if (!targetContainer.CanInsert(inventorySlot.ContainedEntity))
            {
                return false;
            }

            if (!inventorySlot.Remove(inventorySlot.ContainedEntity))
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

            var slot = FindHand(entity);
            if (slot == null)
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
            var inventorySlot = _hands[slot];

            if (ContainerHelpers.TryGetContainer(Owner, out var container) &&
                !container.CanInsert(inventorySlot.ContainedEntity))
            {
                return false;
            }

            return inventorySlot.CanRemove(inventorySlot.ContainedEntity);
        }

        public void AddHand(string index)
        {
            if (HasHand(index))
            {
                throw new InvalidOperationException($"Hand '{index}' already exists.");
            }

            var slot = ContainerManagerComponent.Create<ContainerSlot>(Name + "_" + index, Owner);
            _hands[index] = slot;
            if (!_orderedHands.Contains(index))
            {
                _orderedHands.Add(index);
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

            _hands[index].Shutdown(); //TODO verify this
            _hands.Remove(index);
            _orderedHands.Remove(index);

            if (index == ActiveIndex)
            {
                _activeIndex = _orderedHands.Count == 0 ? null : _orderedHands[0];
            }

            Dirty();
        }

        public bool HasHand(string index)
        {
            return _hands.ContainsKey(index);
        }

        /// <summary>
        ///     Get the name of the slot passed to the inventory component.
        /// </summary>
        private string HandSlotName(string index) => $"_hand_{index}";

        public override ComponentState GetComponentState()
        {
            var dict = new Dictionary<string, EntityUid>(_hands.Count);
            foreach (var hand in _hands)
            {
                if (hand.Value.ContainedEntity != null)
                {
                    dict[hand.Key] = hand.Value.ContainedEntity.Uid;
                }
            }

            return new HandsComponentState(dict, ActiveIndex);
        }

        public void SwapHands()
        {
            var index = _orderedHands.FindIndex(x => x == ActiveIndex);
            index++;
            if (index >= _orderedHands.Count)
            {
                index = 0;
            }

            ActiveIndex = _orderedHands[index];
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

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
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
                        ActiveIndex = msg.Index;
                    break;
                }

                case ClientAttackByInHandMsg msg:
                {
                    if (!_hands.TryGetValue(msg.Index, out var slot))
                    {
                        Logger.WarningS("go.comp.hands", "Got a ClientAttackByInHandMsg with invalid hand index '{0}'",
                            msg.Index);
                        return;
                    }

                    var playerEntity = session.AttachedEntity;
                    var used = GetActiveHand?.Owner;

                    if (playerEntity == Owner && slot.ContainedEntity != null)
                    {
                        var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                        if (used != null)
                        {
                            interactionSystem.Interaction(Owner, used, slot.ContainedEntity,
                                GridCoordinates.InvalidGrid);
                        }
                        else
                        {
                            var entity = slot.ContainedEntity;
                            if (!Drop(entity))
                                break;
                            interactionSystem.Interaction(Owner, entity);
                        }
                    }

                    break;
                }

                case UseInHandMsg msg:
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
            foreach (var container in _hands.Values)
            {
                if (container != message.Container)
                {
                    continue;
                }

                Dirty();

                if (!message.Entity.TryGetComponent(out IPhysicsComponent physics))
                {
                    return;
                }

                // set velocity to zero
                physics.LinearVelocity = Vector2.Zero;
                return;
            }
        }
    }
}
