using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Wires.Components
{
    [RegisterComponent]
    public sealed class WireVisComponent : Component
    {
        public override string Name => "WireVis";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("node")]
        public string? Node;
    }
}
