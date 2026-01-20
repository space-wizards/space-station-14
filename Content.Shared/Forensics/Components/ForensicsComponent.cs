using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

/// <summary>
/// Leaves evidence on entities for <see cref="ForensicScannerComponent"/> to find.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForensicsComponent : Component
{
    /// <summary>
    /// Leaves evidence of FingerPrints left by uncovered hands.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Fingerprints = [];

    /// <summary>
    /// Leaves evidence of Fibers left by gloves.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Fibers = [];

    /// <summary>
    /// Leaves evidence of DNA left by bodily fluids.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> DNAs = [];

    /// <summary>
    /// Leaves evidence of Residues left by cleaning products.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Residues = [];

    /// <summary>
    /// How close you must be to wipe the prints/blood/etc. off of this entity
    /// </summary>
    [DataField]
    public float CleanDistance = 1.5f;

    /// <summary>
    /// Can the DNA be cleaned off of this entity?
    /// e.g. you can wipe the DNA off of a knife, but not a cigarette
    /// </summary>
    [DataField]
    public bool CanDnaBeCleaned = true;
}
