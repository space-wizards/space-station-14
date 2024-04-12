using Content.Shared.Labels.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Labels.Components;

[RegisterComponent, NetworkedComponent]
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

    public Dictionary<NetEntity, LabelAction> RecentlyLabeled = new();
}

[Serializable, NetSerializable]
public sealed class HandLabelerComponentState : ComponentState
{
    public string AssignedLabel { get; init; } = String.Empty;
    public int MaxLabelChars { get; init; }
    public Dictionary<NetEntity, LabelAction> RecentlyLabeled { get; init; } = new();
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
