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
        public SlotFlags AllowedSlots = SlotFlags.MASK | SlotFlags.HEAD;
        public bool IsFunctional;
        public EntityUid? ConnectedInternalsEntity;

        /// <summary>
        /// Tool will automatically turn on internals when it is next equipped
        /// This feature will turn itself off after the first time it's used
        /// </summary>
        [DataField]
        public bool AutomaticActivation = false;
    }
}
