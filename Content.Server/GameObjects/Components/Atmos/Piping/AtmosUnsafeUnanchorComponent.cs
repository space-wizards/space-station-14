using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    [RegisterComponent]
    public class AtmosUnsafeUnanchorComponent : Component
    {
        public override string Name => "AtmosUnsafeUnanchor";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; }
    }
}
