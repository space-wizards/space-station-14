using Content.Shared.Inventory;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    /// Used in internals as breath tool.
    /// </summary>
    [RegisterComponent]
    [ComponentProtoName("BreathMask")]
    public sealed partial class BreathToolComponent : Component
    {
        /// <summary>
        /// Tool is functional only in allowed slots
        /// </summary>
        [DataField("allowedSlots")]
        public SlotFlags AllowedSlots = SlotFlags.MASK;
        public bool IsFunctional;
        public EntityUid? ConnectedInternalsEntity;
    }
}
