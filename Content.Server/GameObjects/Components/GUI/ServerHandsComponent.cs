using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
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

        private string activeIndex;

        [ViewVariables(VVAccess.ReadWrite)]
        public string ActiveIndex
        {
            get => activeIndex;
            set
            {
                if (!hands.ContainsKey(value))
                {
                    throw new ArgumentException($"No hand '{value}'");
                }

                activeIndex = value;
                Dirty();
            }
        }

        [ViewVariables] private Dictionary<string, ContainerSlot> hands = new Dictionary<string, ContainerSlot>();
        [ViewVariables] private List<string> orderedHands = new List<string>();

        // Mostly arbitrary.
        public const float PICKUP_RANGE = 2;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: This does not serialize what objects are held.
            serializer.DataField(ref orderedHands, "hands", new List<string>(0));
            if (serializer.Reading)
            {
                foreach (var handsname in orderedHands)
                {
                    AddHand(handsname);
                }
            }
        }

        public IEnumerable<ItemComponent> GetAllHeldItems()
        {
            foreach (var slot in hands.Values)
            {
                if (slot.ContainedEntity != null)
                {
                    yield return slot.ContainedEntity.GetComponent<ItemComponent>();
                }
            }
        }

        public bool IsHolding(IEntity entity)
        {
            foreach (var slot in hands.Values)
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
            var slot = hands[index];
            return slot.ContainedEntity?.GetComponent<ItemComponent>();
        }

        public ItemComponent GetActiveHand => GetHand(ActiveIndex);

        /// <summary>
        ///     Enumerates over the hand keys, returning the active hand first.
        /// </summary>
        private IEnumerable<string> ActivePriorityEnumerable()
        {
            yield return ActiveIndex;
            foreach (var hand in hands.Keys)
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

            var slot = hands[index];
            Dirty();
            var success = slot.Insert(item.Owner);
            if (success)
            {
                item.Owner.Transform.LocalPosition = Vector2.Zero;
            }

            return success;
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
            var slot = hands[index];
            return slot.CanInsert(item.Owner);
        }

        public string FindHand(IEntity entity)
        {
            foreach (var (index, slot) in hands)
            {
                if (slot.ContainedEntity == entity)
                {
                    return index;
                }
            }

            return null;
        }

        public bool Drop(string slot, GridCoordinates coords)
        {
            if (!CanDrop(slot))
            {
                return false;
            }

            var inventorySlot = hands[slot];
            var item = inventorySlot.ContainedEntity.GetComponent<ItemComponent>();
            if (!inventorySlot.Remove(inventorySlot.ContainedEntity))
            {
                return false;
            }

            item.RemovedFromSlot();

            // TODO: The item should be dropped to the container our owner is in, if any.
            item.Owner.Transform.GridPosition = coords;

            Dirty();
            return true;
        }

        public bool Drop(IEntity entity, GridCoordinates coords)
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

            return Drop(slot, coords);
        }

        public bool Drop(string slot)
        {
            if (!CanDrop(slot))
            {
                return false;
            }

            var inventorySlot = hands[slot];
            var item = inventorySlot.ContainedEntity.GetComponent<ItemComponent>();
            if (!inventorySlot.Remove(inventorySlot.ContainedEntity))
            {
                return false;
            }

            item.RemovedFromSlot();

            // TODO: The item should be dropped to the container our owner is in, if any.
            item.Owner.Transform.GridPosition = Owner.Transform.GridPosition;
            if (item.Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
            {
                spriteComponent.RenderOrder = item.Owner.EntityManager.CurrentTick.Value;
            }

            Dirty();
            return true;
        }

        public bool Drop(IEntity entity)
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

            return Drop(slot);
        }

        public bool Drop(string slot, BaseContainer targetContainer)
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

            var inventorySlot = hands[slot];
            var item = inventorySlot.ContainedEntity.GetComponent<ItemComponent>();
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

        public bool Drop(IEntity entity, BaseContainer targetContainer)
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

            return Drop(slot, targetContainer);
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
            var inventorySlot = hands[slot];
            return inventorySlot.CanRemove(inventorySlot.ContainedEntity);
        }

        public void AddHand(string index)
        {
            if (HasHand(index))
            {
                throw new InvalidOperationException($"Hand '{index}' already exists.");
            }

            var slot = ContainerManagerComponent.Create<ContainerSlot>(Name + "_" + index, Owner);
            hands[index] = slot;
            if (!orderedHands.Contains(index))
            {
                orderedHands.Add(index);
            }

            if (ActiveIndex == null)
            {
                ActiveIndex = index;
            }

            Dirty();
        }

        public void RemoveHand(string index)
        {
            if (!HasHand(index))
            {
                throw new InvalidOperationException($"Hand '{index}' does not exist.");
            }

            hands[index].Shutdown(); //TODO verify this
            hands.Remove(index);
            orderedHands.Remove(index);

            if (index == ActiveIndex)
            {
                if (orderedHands.Count == 0)
                {
                    activeIndex = null;
                }
                else
                {
                    activeIndex = orderedHands[0];
                }
            }

            Dirty();
        }

        public bool HasHand(string index)
        {
            return hands.ContainsKey(index);
        }

        /// <summary>
        ///     Get the name of the slot passed to the inventory component.
        /// </summary>
        private string HandSlotName(string index) => $"_hand_{index}";

        public override ComponentState GetComponentState()
        {
            var dict = new Dictionary<string, EntityUid>(hands.Count);
            foreach (var hand in hands)
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
            var index = orderedHands.FindIndex(x => x == ActiveIndex);
            index++;
            if (index >= orderedHands.Count)
            {
                index = 0;
            }

            ActiveIndex = orderedHands[index];
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

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null,
            IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case ClientChangedHandMsg msg:
                {
                    var playerMan = IoCManager.Resolve<IPlayerManager>();
                    var session = playerMan.GetSessionByChannel(netChannel);
                    var playerEntity = session.AttachedEntity;

                    if (playerEntity == Owner && HasHand(msg.Index))
                        ActiveIndex = msg.Index;
                    break;
                }

                case ClientAttackByInHandMsg msg:
                {
                    if (!hands.TryGetValue(msg.Index, out var slot))
                    {
                        Logger.WarningS("go.comp.hands", "Got a ClientAttackByInHandMsg with invalid hand index '{0}'",
                            msg.Index);
                        return;
                    }

                    var playerMan = IoCManager.Resolve<IPlayerManager>();
                    var session = playerMan.GetSessionByChannel(netChannel);
                    var playerEntity = session.AttachedEntity;
                    var used = GetActiveHand?.Owner;

                    if (playerEntity == Owner && used != null && slot.ContainedEntity != null)
                        {
                        var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                        interactionSystem.Interaction(Owner, used, slot.ContainedEntity,
                            GridCoordinates.Nullspace);
                    }

                    break;
                }

                case ActivateInhandMsg msg:
                {
                    var playerMan = IoCManager.Resolve<IPlayerManager>();
                    var session = playerMan.GetSessionByChannel(netChannel);
                    var playerEntity = session.AttachedEntity;
                    var used = GetActiveHand?.Owner;

                    if (playerEntity == Owner && used != null)
                    {
                        var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                        interactionSystem.TryUseInteraction(Owner, used);
                    }

                    break;
                }
            }
        }

        public void HandleSlotModifiedMaybe(ContainerModifiedMessage message)
        {
            foreach (var container in hands.Values)
            {
                if (container != message.Container)
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
    }
}
