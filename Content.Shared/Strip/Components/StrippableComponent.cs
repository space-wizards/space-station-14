using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Strip.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class StrippableComponent : Component
    {
        /// <summary>
        /// The strip delay for hands.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("handDelay")]
        public float HandStripDelay = 4f;
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
    /// <remarks>
    /// This is also used by some stripping related interactions, i.e., interactions with items that are currently equipped by another player.
    /// </remarks>
    public sealed class BeforeStripEvent : BaseBeforeStripEvent
    {
        public BeforeStripEvent(float initialTime, bool stealth = false) : base(initialTime, stealth) { }
    }

    /// <summary>
    /// Used to modify strip times. Raised directed at the target.
    /// </summary>
    /// <remarks>
    /// This is also used by some stripping related interactions, i.e., interactions with items that are currently equipped by another player.
    /// </remarks>
    public sealed class BeforeGettingStrippedEvent : BaseBeforeStripEvent
    {
        public BeforeGettingStrippedEvent(float initialTime, bool stealth = false) : base(initialTime, stealth) { }
    }
}
