using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Implants.Components
{
    /// <summary>
    /// Component used for tracking which head revolutionary owns a USSP uplink.
    /// This allows us to correctly associate telebonds with the specific head revolutionary
    /// who earned them, even when the uplink is implanted in a regular revolutionary.
    /// </summary>
    [RegisterComponent]
    public sealed partial class USSPUplinkOwnerComponent : Component
    {
        /// <summary>
        /// The entity UID of the head revolutionary who owns this uplink.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public EntityUid? OwnerUid;
    }
}
