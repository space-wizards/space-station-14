using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared.Strip.Components
{
    public abstract class SharedStrippableComponent : Component, IDraggable
    {
        public bool CanBeStripped(EntityUid by)
        {
            return by != Owner
                   && IoCManager.Resolve<IEntityManager>().HasComponent<SharedHandsComponent>(@by)
                   && EntitySystem.Get<ActionBlockerSystem>().CanInteract(@by, Owner);
        }

        bool IDraggable.CanDrop(CanDropEvent args)
        {
            return args.Target != args.Dragged
                   && args.Target == args.User
                   && CanBeStripped(args.User);
        }

        public abstract bool Drop(DragDropEvent args);
    }

    [NetSerializable, Serializable]
    public enum StrippingUiKey : byte
    {
        Key,
    }

    [NetSerializable, Serializable]
    public sealed class StrippingInventoryButtonPressed : BoundUserInterfaceMessage
    {
        public string Slot { get; }

        public StrippingInventoryButtonPressed(string slot)
        {
            Slot = slot;
        }
    }

    [NetSerializable, Serializable]
    public sealed class StrippingHandButtonPressed : BoundUserInterfaceMessage
    {
        public string Hand { get; }

        public StrippingHandButtonPressed(string hand)
        {
            Hand = hand;
        }
    }

    [NetSerializable, Serializable]
    public sealed class StrippingHandcuffButtonPressed : BoundUserInterfaceMessage
    {
        public EntityUid Handcuff { get; }

        public StrippingHandcuffButtonPressed(EntityUid handcuff)
        {
            Handcuff = handcuff;
        }
    }

    [NetSerializable, Serializable]
    public sealed class StrippingBoundUserInterfaceState : BoundUserInterfaceState
    {
        public Dictionary<(string ID, string Name), string> Inventory { get; }
        public Dictionary<string, string> Hands { get; }
        public Dictionary<EntityUid, string> Handcuffs { get; }

        public StrippingBoundUserInterfaceState(Dictionary<(string ID, string Name), string> inventory, Dictionary<string, string> hands, Dictionary<EntityUid, string> handcuffs)
        {
            Inventory = inventory;
            Hands = hands;
            Handcuffs = handcuffs;
        }
    }

    /// <summary>
    /// Used to modify strip times.
    /// </summary>
    [NetSerializable, Serializable]
    public sealed class BeforeStripEvent : EntityEventArgs, IInventoryRelayEvent
    {
        public readonly float InitialTime;
        public float Time;
        public float Additive = 0;
        public bool Stealth;

        public SlotFlags TargetSlots { get; } = SlotFlags.GLOVES;

        public BeforeStripEvent(float initialTime)
        {
            InitialTime = Time = initialTime;
        }
    }
}
