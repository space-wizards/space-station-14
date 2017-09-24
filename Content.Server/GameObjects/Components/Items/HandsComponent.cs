using Content.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Content.Server.GameObjects
{
    public class HandsComponent : Component, IHandsComponent
    {
        public override string Name => "Hands";

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
        private IInventoryComponent inventory;

        public override void Initialize()
        {
            inventory = Owner.GetComponent<IInventoryComponent>();
        }

        public IEnumerable<IItemComponent> GetAllHands()
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
    }
}
