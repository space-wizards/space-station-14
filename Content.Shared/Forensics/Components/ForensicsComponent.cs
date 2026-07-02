using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForensicsComponent : Component
{
    [DataField]
    public HashSet<string> Fingerprints = [];

    [DataField]
    public HashSet<string> Fibers = [];

    [DataField]
    public HashSet<string> DNAs = [];

    [DataField]
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

    /// <summary>
    /// Whether this entity is currently cleanable.
    /// Solely used for client-side prediction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsDirty;
}
