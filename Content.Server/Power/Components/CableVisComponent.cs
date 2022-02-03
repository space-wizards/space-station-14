using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed class CableVisComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("node")]
        public string? Node;
    }
}
