using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.BarSign
{
    [RegisterComponent]
    public class BarSignComponent : Component
    {
        [DataField("current")]
        [ViewVariables(VVAccess.ReadOnly)]
        public string? CurrentSign;
    }
}
