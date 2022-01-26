using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.PowerCell;

/// <summary>
///     This component enables power-cell related interactions (e.g., entity white-lists, cell sizes, examine, rigging).
///     The actual power functionality is provided by the server-side BatteryComponent.
/// </summary>
[NetworkedComponent]
[RegisterComponent]
[ComponentProtoName("PowerCell")]
public sealed class PowerCellComponent : Component
{
    public const string SolutionName = "powerCell";
    public const int PowerCellVisualsLevels = 4;

    [DataField("cellSize")]
    public PowerCellSize CellSize = PowerCellSize.Small;

    // Not networked to clients
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsRigged { get; set; }
}

public enum PowerCellSize
{
    Small = 0,
    Medium = 1,
    Large = 2
}

[Serializable, NetSerializable]
public enum PowerCellVisuals
{
    ChargeLevel
}
