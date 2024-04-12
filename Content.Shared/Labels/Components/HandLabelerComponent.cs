
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Labels.Components;

[RegisterComponent,NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedHandLabelerSystem))]
public sealed partial class HandLabelerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    [AutoNetworkedField]
    public string AssignedLabel = string.Empty;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    [AutoNetworkedField]
    public int MaxLabelChars = 50;

    [DataField]
    public EntityWhitelist Whitelist = new();
}

/// <summary>
/// Different actions the HandLabeler can do.
/// </summary>
/// <remarks>
/// `invalid` value should never appear anywhere.
/// <see cref="HandLabelerMessage">
/// </remarks>
public enum LabelAction
{
    invalid,
    Removed,
    Applied
}
