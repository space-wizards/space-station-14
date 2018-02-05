using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using SS14.Server.GameObjects.Events;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using SS14.Shared.Enums;

namespace Content.Server.GameObjects
{
    public class HandsComponent : SharedHandsComponent, IHandsComponent
    {
        private string activeIndex;
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
            }
        }

        private Dictionary<string, IInventorySlot> hands = new Dictionary<string, IInventorySlot>();
        private List<string> orderedHands = new List<string>();
        private IInventoryComponent inventory;
        private IServerTransformComponent transform;
        private YamlMappingNode tempParametersMapping;

        // Mostly arbitrary.
        public const float PICKUP_RANGE = 2;

        public override void Initialize()
        {
            inventory = Owner.GetComponent<IInventoryComponent>();
            transform = Owner.GetComponent<IServerTransformComponent>();
            if (tempParametersMapping != null)
            {
                foreach (var node in tempParametersMapping.GetNode<YamlSequenceNode>("hands"))
                {
                    AddHand(node.AsString());
                }
            }

            Owner.SubscribeEvent<BoundKeyChangeEventArgs>(OnKeyChange, this);
            base.Initialize();
        }

        public override void OnRemove()
        {
            inventory = null;
            Owner.UnsubscribeEvent<BoundKeyChangeEventArgs>(this);
            Owner.UnsubscribeEvent<ClickedOnEntityEventArgs>(this);
            base.OnRemove();
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            tempParametersMapping = mapping;
            base.LoadParameters(mapping);
        }

        public IEnumerable<IItemComponent> GetAllHeldItems()
        {
            foreach (var slot in hands.Values)
            {
                if (slot.Item != null)
                {
                    yield return slot.Item;
                }
            }
        }

        public IItemComponent GetHand(string index)
        {
            var slot = hands[index];
            return slot.Item;
        }

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

        public bool PutInHand(IItemComponent item)
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

        public bool PutInHand(IItemComponent item, string index, bool fallback = true)
        {
            if (!CanPutInHand(item, index))
            {
                return fallback && PutInHand(item);
            }

            var slot = hands[index];
            return slot.Owner.Insert(slot.Name, item);
        }

        public bool CanPutInHand(IItemComponent item)
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

        public bool CanPutInHand(IItemComponent item, string index)
        {
            var slot = hands[index];
            return slot.Owner.CanInsert(slot.Name, item);
        }

        public bool Drop(string index)
        {
            if (!CanDrop(index))
            {
                return false;
            }

            var slot = hands[index];
            return slot.Owner.Drop(slot.Name);
        }

        public bool CanDrop(string index)
        {
            var slot = hands[index];
            return slot.Item != null && slot.Owner.CanDrop(slot.Name);
        }

        public void AddHand(string index)
        {
            if (HasHand(index))
            {
                throw new InvalidOperationException($"Hand '{index}' already exists.");
            }

            var slot = inventory.AddSlot(HandSlotName(index));
            hands[index] = slot;
            orderedHands.Add(index);
            if (ActiveIndex == null)
            {
                ActiveIndex = index;
            }
        }

        public void RemoveHand(string index)
        {
            if (!HasHand(index))
            {
                throw new InvalidOperationException($"Hand '{index}' does not exist.");
            }

            inventory.RemoveSlot(HandSlotName(index));
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
                if (hand.Value.Item != null)
                {
                    dict[hand.Key] = hand.Value.Item.Owner.Uid;
                }
            }
            return new HandsComponentState(dict, ActiveIndex);
        }

        // Game logic goes here.
        public void OnKeyChange(object sender, EntityEventArgs uncast)
        {
            var cast = (BoundKeyChangeEventArgs)uncast;
            if (cast.Actor != Owner || cast.KeyState != BoundKeyState.Down)
            {
                return;
            }

            switch (cast.KeyFunction)
            {
                case BoundKeyFunctions.SwitchHands:
                    SwapHands();
                    break;
                case BoundKeyFunctions.Drop:
                    Drop(ActiveIndex);
                    break;
            }
        }

        private void SwapHands()
        {
            var index = orderedHands.FindIndex(x => x == ActiveIndex);
            index++;
            if (index >= orderedHands.Count)
            {
                index = 0;
            }

            ActiveIndex = orderedHands[index];
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            if (message.MessageParameters.Count != 1)
            {
                return;
            }
            var index = message.MessageParameters[0];
            if (index is string newIndex && HasHand(newIndex))
            {
                ActiveIndex = newIndex;
            }
            base.HandleNetworkMessage(message);
        }
    }
}
