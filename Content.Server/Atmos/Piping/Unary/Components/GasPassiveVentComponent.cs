using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public class GasPassiveVentComponent : Component
    {
        [DataField("inlet")]
        public string InletName = "pipe";
    }
}
