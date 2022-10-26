using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared.Strip.Components
{
    public abstract class SharedStrippableComponent : Component, IDraggable
    {
        bool IDraggable.CanDrop(CanDropEvent args)
        {
            var ent = IoCManager.Resolve<IEntityManager>();
            return args.Target != args.Dragged &&
                args.Target == args.User &&
                ent.HasComponent<SharedStrippingComponent>(args.User) &&
                ent.HasComponent<SharedHandsComponent>(args.User) &&
                ent.EntitySysManager.GetEntitySystem<ActionBlockerSystem>().CanInteract(args.User, args.Dragged);
        }

        public abstract bool Drop(DragDropEvent args);
    }

    [NetSerializable, Serializable]
    public enum StrippingUiKey : byte
    {
        Key,
    }

    [NetSerializable, Serializable]
    public sealed class StrippingSlotButtonPressed : BoundUserInterfaceMessage
    {
        public readonly string Slot;

        public readonly bool IsHand;

        public StrippingSlotButtonPressed(string slot, bool isHand)
        {
            Slot = slot;
            IsHand = isHand;
        }
    }

    [NetSerializable, Serializable]
    public sealed class StrippingEnsnareButtonPressed : BoundUserInterfaceMessage
    {
        public StrippingEnsnareButtonPressed()
        {
        }
    }

    public abstract class BaseBeforeStripEvent : EntityEventArgs, IInventoryRelayEvent
    {
        public readonly float InitialTime;
        public float Time => MathF.Max(InitialTime * Multiplier + Additive, 0f);
        public float Additive = 0;
        public float Multiplier = 1f;
        public bool Stealth;

        public SlotFlags TargetSlots { get; } = SlotFlags.GLOVES;

        public BaseBeforeStripEvent(float initialTime, bool stealth = false)
        {
            InitialTime = initialTime;
            Stealth = stealth;
        }
    }

    /// <summary>
    /// Used to modify strip times. Raised directed at the user.
    /// </summary>
    public sealed class BeforeStripEvent : BaseBeforeStripEvent
    {
        public BeforeStripEvent(float initialTime, bool stealth = false) : base(initialTime, stealth) { }
    }

    /// <summary>
    /// Used to modify strip times. Raised directed at the target.
    /// </summary>
    public sealed class BeforeGettingStrippedEvent : BaseBeforeStripEvent
    {
        public BeforeGettingStrippedEvent(float initialTime, bool stealth = false) : base(initialTime, stealth) { }
    }
}
