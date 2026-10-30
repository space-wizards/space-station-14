using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Power.Components;

/// <summary>
/// Allows the attached entity to be destroyed by a cutting tool, dropping a piece of cable.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CableComponent : Component
{
    [DataField]
    public EntProtoId CableDroppedOnCutPrototype = "CableHVStack1";

    /// <summary>
    /// The tool quality needed to cut the cable. Setting to null prevents cutting.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype>? CuttingQuality = SharedToolSystem.CutQuality;

    /// <summary>
    /// Checked by <c>CablePlacerComponent</c> to determine if there is
    /// already a cable of a type on a tile.
    /// </summary>
    [DataField]
    public CableType CableType = CableType.HighVoltage;

    [DataField]
    public TimeSpan CuttingDelay = TimeSpan.FromSeconds(1);
}
