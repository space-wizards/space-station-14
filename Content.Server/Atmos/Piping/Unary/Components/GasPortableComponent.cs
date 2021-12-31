using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public class GasPortableComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("port")]
        public string PortName { get; set; } = "port";
    }
}
