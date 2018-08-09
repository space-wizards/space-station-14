using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using Content.Shared.Input;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.Components.Container;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Players;
using SS14.Shared.Serialization;

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
                Dirty();
            }
        }

        private Dictionary<string, ContainerSlot> hands = new Dictionary<string, ContainerSlot>();
        private List<string> orderedHands = new List<string>();

        // Mostly arbitrary.
        public const float PICKUP_RANGE = 2;

        private InputCmdHandler _swapHandsCmdHandler;
        private InputCmdHandler _dropCmdHandler;
        private InputCmdHandler _activateItemInHandCmdHandler;

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
            return slot.Insert(item.Owner);
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

        /// <summary>
        ///     Drops the item in a slot.
        /// </summary>
        /// <param name="slot">The slot to drop the item from.</param>
        /// <returns>True if an item was dropped, false otherwise.</returns>
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
            var itemTransform = item.Owner.GetComponent<ITransformComponent>();
            itemTransform.LocalPosition = Owner.GetComponent<ITransformComponent>().LocalPosition;
            Dirty();
            return true;
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

        private void SwapHands(ICommonSession channel)
        {
            var index = orderedHands.FindIndex(x => x == ActiveIndex);
            index++;
            if (index >= orderedHands.Count)
            {
                index = 0;
            }

            ActiveIndex = orderedHands[index];
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            IPlayerInput input;
            switch (message)
            {
                case ClientChangedHandMsg msg:
                    {
                        var playerMan = IoCManager.Resolve<IPlayerManager>();
                        var session = playerMan.GetSessionByChannel(netChannel);
                        var playerentity = session.AttachedEntity;

                        if (playerentity == Owner && HasHand(msg.Index))
                            ActiveIndex = msg.Index;
                        break;
                    }

                case ActivateInhandMsg msg:
                    {
                        var playerMan = IoCManager.Resolve<IPlayerManager>();
                        var session = playerMan.GetSessionByChannel(netChannel);
                        var playerentity = session.AttachedEntity;
                        var used = GetActiveHand?.Owner;

                        if (playerentity == Owner && used != null)
                        {
                            InteractionSystem.TryUseInteraction(Owner, used);
                        }
                        break;
                    }

                //Boundkeychangedmsg only works for the player entity and doesn't need any extra verification
                case PlayerAttachedMsg msg:
                    InitInputCommands();
                    input = msg.NewPlayer.Input;
                    input.SetCommand(ContentKeyFunctions.SwapHands, _swapHandsCmdHandler);
                    input.SetCommand(ContentKeyFunctions.Drop, _dropCmdHandler);
                    input.SetCommand(ContentKeyFunctions.ActivateItemInHand, _activateItemInHandCmdHandler);
                    break;

                case PlayerDetachedMsg msg:
                    input = msg.OldPlayer.Input;
                    input.SetCommand(ContentKeyFunctions.SwapHands, null);
                    input.SetCommand(ContentKeyFunctions.Drop, null);
                    input.SetCommand(ContentKeyFunctions.ActivateItemInHand, null);
                    break;
            }
        }

        private void InitInputCommands()
        {
            if (_swapHandsCmdHandler != null)
            {
                return;
            }

            _swapHandsCmdHandler = InputCmdHandler.FromDelegate(SwapHands);
            _dropCmdHandler = InputCmdHandler.FromDelegate(session => Drop(ActiveIndex));
            _activateItemInHandCmdHandler = InputCmdHandler.FromDelegate(session =>
            {
                var used = GetActiveHand?.Owner;
                if (used != null)
                {
                    InteractionSystem.TryUseInteraction(Owner, used);
                }
            });
        }
    }
}
