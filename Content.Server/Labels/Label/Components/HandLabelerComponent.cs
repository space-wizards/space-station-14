using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Labels.Components
{
    [RegisterComponent]
    public class HandLabelerComponent : Component
    {
        public override string Name => "HandLabeler";

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
