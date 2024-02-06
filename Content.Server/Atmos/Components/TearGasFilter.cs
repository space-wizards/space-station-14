///using Content.Shared.Inventory;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    /// Used in breath tool with a smoke gas filter.
    /// </summary>
    [RegisterComponent]
    [ComponentProtoName("FilterMask")]
    public sealed partial class SmokeFilterComponent : Component
    {
        ///[DataField("allowedSlots")]
        ///public SlotFlags AllowedSlots = SlotFlags.MASK | SlotFlags.HEAD;
        [DataField("Filter Active")]
        public bool IsActive = false;
    }
}
