using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Revolutionary.Components
{
    /// <summary>
    /// Component used for tracking which head revolutionary converted a revolutionary.
    /// This allows us to correctly enforce the rule that a head revolutionary can only
    /// implant revolutionaries they converted, and revolutionaries can only use implanters
    /// from their converter.
    /// </summary>
    [RegisterComponent]
    public sealed partial class RevolutionaryConverterComponent : Component
    {
        /// <summary>
        /// The entity UID of the head revolutionary who converted this revolutionary.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public EntityUid? ConverterUid;
    }
}
