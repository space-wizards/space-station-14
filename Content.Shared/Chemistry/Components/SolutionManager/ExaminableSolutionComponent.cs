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

    [DataField]
    public LocId MessageEmptyVolume = "shared-solution-container-component-on-examine-empty-container";

    [DataField]
    public LocId ShortMessageEmptyVolume = "drink-component-on-examine-is-empty";

    [DataField]
    public LocId MessageExactVolume = "drink-component-on-examine-exact-volume";

    [DataField]
    public LocId MessageVagueVolumeFull = "drink-component-on-examine-is-full";

    [DataField]
    public LocId MessageVagueVolumeMostlyFull = "drink-component-on-examine-is-mostly-full";

    [DataField]
    public LocId MessageVagueVolumeHalfFull = "drink-component-on-examine-is-half-full";

    [DataField]
    public LocId MessageVagueVolumeHalfEmpty = "drink-component-on-examine-is-half-empty";

    [DataField]
    public LocId MessageVagueVolumeMostlyEmpty = "drink-component-on-examine-is-mostly-empty";

    [DataField]
    public LocId MessagePhysicalQuality = "shared-solution-container-component-on-examine-main-text";

    [DataField]
    public LocId MessageRecognizableReagents = "examinable-solution-has-recognizable-chemicals";
}

/// <summary>
///     The
/// </summary>
[Serializable, NetSerializable]
public enum ExaminedVolumeState
{
    Exact,
    Full,
    MostlyFull,
    HalfFull,
    HalfEmpty,
    MostlyEmpty,
    Empty,
}
