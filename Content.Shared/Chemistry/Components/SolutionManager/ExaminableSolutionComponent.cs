using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components.SolutionManager;

/// <summary>
///     Component for examining a solution with shift click or through <see cref="SolutionScanEvent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class ExaminableSolutionComponent : Component
{
    /// <summary>
    ///     The solution being examined.
    /// </summary>
    [DataField]
    public string Solution = "default";

    /// <summary>
    ///     If true, the solution must be held to be examined.
    /// </summary>
    [DataField]
    public bool HeldOnly;

    /// <summary>
    ///     If false, the examine text will give an approximation of the remaining solution.
    ///     If true, the exact unit count will be shown.
    /// </summary>
    [DataField]
    public bool ExactVolume;

    /// <summary>
    ///     If true, the solution can't be examined when this entity is closed.
    /// </summary>
    public bool Opaque;

    [DataField]
    public LocId LocEmptyVolume = "shared-solution-container-component-on-examine-empty-container";

    [DataField]
    public LocId LocVolume = "examinable-solution-on-examine-volume";

    [DataField]
    public LocId LocPhysicalQuality = "shared-solution-container-component-on-examine-main-text";

    [DataField]
    public LocId LocRecognizableReagents = "examinable-solution-has-recognizable-chemicals";
}

/// <summary>
///     Used to choose how to display a volume.
/// </summary>
[Serializable, NetSerializable]
public enum ExaminedVolumeDisplay
{
    Exact,
    Full,
    MostlyFull,
    HalfFull,
    HalfEmpty,
    MostlyEmpty,
    Empty,
}
