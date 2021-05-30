#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Shared.GameObjects.Components.GUI
{
    public abstract class SharedStrippableComponent : Component, IDraggable
    {
        public override string Name => "Strippable";

        public bool CanBeStripped(IEntity by)
        {
            return by != Owner
                   && by.HasComponent<ISharedHandsComponent>()
                   && ActionBlockerSystem.CanInteract(by);
        }

        bool IDraggable.CanDrop(CanDropEvent args)
        {
            return args.Target != args.Dragged
                   && args.Target == args.User
                   && CanBeStripped(args.User);
        }

        public abstract bool Drop(DragDropEvent args);

        [NetSerializable, Serializable]
        public enum StrippingUiKey
        {
            Key,
        }
    }

    [NetSerializable, Serializable]
    public class StrippingInventoryButtonPressed : BoundUserInterfaceMessage
    {
        public Slots Slot { get; }

        public StrippingInventoryButtonPressed(Slots slot)
        {
            Slot = slot;
        }
    }

    [NetSerializable, Serializable]
    public class StrippingHandButtonPressed : BoundUserInterfaceMessage
    {
        public string Hand { get; }

        public StrippingHandButtonPressed(string hand)
        {
            Hand = hand;
        }
    }

    [NetSerializable, Serializable]
    public class StrippingHandcuffButtonPressed : BoundUserInterfaceMessage
    {
        public EntityUid Handcuff { get; }

        public StrippingHandcuffButtonPressed(EntityUid handcuff)
        {
            Handcuff = handcuff;
        }
    }

    [NetSerializable, Serializable]
    public class StrippingBoundUserInterfaceState : BoundUserInterfaceState
    {
        public Dictionary<Slots, string> Inventory { get; }
        public Dictionary<string, string> Hands { get; }
        public Dictionary<EntityUid, string> Handcuffs { get; }

        public StrippingBoundUserInterfaceState(Dictionary<Slots, string> inventory, Dictionary<string, string> hands, Dictionary<EntityUid, string> handcuffs)
        {
            Inventory = inventory;
            Hands = hands;
            Handcuffs = handcuffs;
        }
    }
}
