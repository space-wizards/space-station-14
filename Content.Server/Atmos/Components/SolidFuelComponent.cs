using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Components;

/// <summary>
/// Material which accumulates heat from nearby ignition sources and is consumed while burning.
/// Times are seconds; ignition time is measured against a smouldering cigarette (rate 1).
/// </summary>
[RegisterComponent]
public sealed partial class SolidFuelComponent : Component
{
    /// <summary>Exposure required to ignite, in cigarette-equivalent seconds.</summary>
    [DataField] public float IgnitionTime = 90f;
    /// <summary>Seconds of burning before the material is consumed at the default fuel rate.</summary>
    [DataField] public float BurnTime = 60f;
    /// <summary>Exposure lost per second when no ignition source is heating the material.</summary>
    [DataField] public float CoolingRate = 2f;
    /// <summary>Entity spawned when the material is fully consumed.</summary>
    [DataField] public EntProtoId AshPrototype = "Ash";

    /// <summary>Accumulated heat exposure, in cigarette-equivalent seconds.</summary>
    [DataField] public float Exposure;
    /// <summary>Accumulated burning time, scaled by the fuel consumption multiplier.</summary>
    [DataField] public float BurnedTime;

    /// <summary>Seconds of wetness remaining, independent of stacks added by incendiary weapons.</summary>
    [DataField] public float WetTime;

    /// <summary>Original tile type for transient floor fuel entities; null for ordinary objects.</summary>
    [DataField] public ProtoId<ContentTileDefinition>? TileType;
}
