using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Labels.Components
{
    [RegisterComponent]
    public class LabelComponent : Component
    {
        public override string Name => "Label";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("currentLabel")]
        public string? CurrentLabel { get; set; }

        public string? OriginalName { get; set; }
    }
}
