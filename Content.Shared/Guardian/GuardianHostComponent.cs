using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Guardian
{
    /// <summary>
    /// Given to guardian users upon establishing a guardian link with the entity
    /// </summary>
    [RegisterComponent]
    public sealed partial class GuardianHostComponent : Component
    {
        /// <summary>
        /// Guardian hosted within the component
        /// </summary>
        /// <remarks>
        /// Can be null if the component is added at any time.
        /// </remarks>
        [DataField]
        public EntityUid? HostedGuardian;

        /// <summary>
        /// Container which holds the guardian
        /// </summary>
        [ViewVariables] public ContainerSlot GuardianContainer = default!;

        [DataField]
        public EntProtoId Action = "ActionToggleGuardian";

        [DataField] public EntityUid? ActionEntity;
    }
}
