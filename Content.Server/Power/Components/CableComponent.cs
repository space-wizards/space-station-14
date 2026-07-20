using Content.Server.Power.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Power.Components;

/// <summary>
///     Allows the attached entity to be destroyed by a cutting tool, dropping a piece of cable.
/// </summary>
[RegisterComponent]
[Access(typeof(CableSystem))]
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
    ///     Checked by <see cref="CablePlacerComponent"/> to determine if there is
    ///     already a cable of a type on a tile.
    /// </summary>
    [DataField]
    public CableType CableType = CableType.HighVoltage;

    [DataField]
    public float CuttingDelay = 1f;
}

/// <summary>
///     Event to be raised when a cable is anchored / unanchored
/// </summary>
[ByRefEvent]
public readonly struct CableAnchorStateChangedEvent
{
    public readonly Entity<TransformComponent> Cable;
    public bool Anchored => Cable.Comp.Anchored;

    /// <summary>
    ///     If true, the entity is being detached to null-space
    /// </summary>
    public readonly bool Detaching;

    public CableAnchorStateChangedEvent(Entity<TransformComponent> cable, bool detaching = false)
    {
        Cable = cable;
        Detaching = detaching;
    }
}
