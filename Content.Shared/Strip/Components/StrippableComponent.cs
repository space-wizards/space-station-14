using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Strip.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class StrippableComponent : Component
    {
        /// <summary>
        ///     The strip delay for hands.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("handDelay")]
        public TimeSpan HandStripDelay = TimeSpan.FromSeconds(4f);
    }

    [NetSerializable, Serializable]
    public enum StrippingUiKey : byte
    {
        Key,
    }

    [NetSerializable, Serializable]
    public sealed class StrippingSlotButtonPressed(string slot, bool isHand) : BoundUserInterfaceMessage
    {
        public readonly string Slot = slot;
        public readonly bool IsHand = isHand;
    }

    [NetSerializable, Serializable]
    public sealed class StrippingEnsnareButtonPressed : BoundUserInterfaceMessage;

    [ByRefEvent]
    public abstract class BaseBeforeStripEvent(TimeSpan initialTime, bool stealth = false) : EntityEventArgs, IInventoryRelayEvent
    {
        public readonly TimeSpan InitialTime = initialTime;
        public float Multiplier = 1f;
        public TimeSpan Additive = TimeSpan.Zero;
        public bool Stealth = stealth;

        public TimeSpan Time => TimeSpan.FromSeconds(MathF.Max(InitialTime.Seconds * Multiplier + Additive.Seconds, 0f));

        public SlotFlags TargetSlots { get; } = SlotFlags.GLOVES;
    }

    /// <summary>
    ///     Used to modify strip times. Raised directed at the user.
    /// </summary>
    /// <remarks>
    ///     This is also used by some stripping related interactions, i.e., interactions with items that are currently equipped by another player.
    /// </remarks>
    [ByRefEvent]
    public sealed class BeforeStripEvent(TimeSpan initialTime, bool stealth = false) : BaseBeforeStripEvent(initialTime, stealth);

    /// <summary>
    ///     Used to modify strip times. Raised directed at the target.
    /// </summary>
    /// <remarks>
    ///     This is also used by some stripping related interactions, i.e., interactions with items that are currently equipped by another player.
    /// </remarks>
    [ByRefEvent]
    public sealed class BeforeGettingStrippedEvent(TimeSpan initialTime, bool stealth = false) : BaseBeforeStripEvent(initialTime, stealth);

    /// <summary>
    ///     Organizes the behavior of DoAfters for <see cref="StrippableSystem">.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed partial class StrippableDoAfterEvent : DoAfterEvent
    {
        public readonly bool InsertOrRemove;
        public readonly bool InventoryOrHand;
        public readonly string SlotOrHandName;

        public StrippableDoAfterEvent(bool insertOrRemove, bool inventoryOrHand, string slotOrHandName)
        {
            InsertOrRemove = insertOrRemove;
            InventoryOrHand = inventoryOrHand;
            SlotOrHandName = slotOrHandName;
        }

        public override DoAfterEvent Clone() => this;
    }
}
