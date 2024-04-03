
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Labels.Components;

[RegisterComponent,NetworkedComponent]
[Access(typeof(SharedHandLabelerSystem))]
public sealed partial class HandLabelerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public string AssignedLabel = string.Empty;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int MaxLabelChars = 50;

    [DataField]
    public EntityWhitelist Whitelist = new();
}
