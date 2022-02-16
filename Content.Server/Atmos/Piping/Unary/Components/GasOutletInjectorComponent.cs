using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed class GasOutletInjectorComponent : Component
    {

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Injecting { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public float VolumeRate { get; set; } = 50f;

        [DataField("inlet")]
        public string InletName { get; set; } = "pipe";

        // TODO ATMOS: Inject method.
    }
}
