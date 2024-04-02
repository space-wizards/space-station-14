
using Content.Shared.Whitelist;

namespace Content.Shared.Labels.Components
{
    [RegisterComponent]
    public sealed partial class HandLabelerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("assignedLabel")]
        public string AssignedLabel { get; set; } = string.Empty;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxLabelChars")]
        public int MaxLabelChars { get; set; } = 50;

        [DataField("whitelist")]
        public EntityWhitelist Whitelist = new();
    }
}
