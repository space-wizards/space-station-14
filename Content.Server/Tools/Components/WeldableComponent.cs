using Content.Server.Tools.Systems;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tools.Components;

/// <summary>
///     Allows users to weld/unweld doors, crates and lockers.
/// </summary>
[RegisterComponent]
[Access(typeof(WeldableSystem))]
public sealed class WeldableComponent : SharedWeldableComponent
{
    /// <summary>
    ///     Tool quality for welding.
    /// </summary>
    [DataField("weldingQuality", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public string WeldingQuality = "Welding";

    /// <summary>
    ///     Whether this entity can ever be welded shut.
    /// </summary>
    [DataField("weldable")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Weldable = true;

    /// <summary>
    ///     How much fuel does it take to weld/unweld entity.
    /// </summary>
    [DataField("fuel")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float FuelConsumption = 1f;

    /// <summary>
    ///     How much time does it take to weld/unweld entity.
    /// </summary>
    [DataField("time")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan WeldingTime = TimeSpan.FromSeconds(1f);

    /// <summary>
    ///     Shown when welded entity is examined.
    /// </summary>
    [DataField("weldedExamineMessage")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? WeldedExamineMessage = "weldable-component-examine-is-welded";

    /// <summary>
    ///     Whether something is currently using a welder on this so DoAfter isn't spammed.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool BeingWelded;

    /// <summary>
    ///     Is this entity currently welded shut?
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsWelded;
}
