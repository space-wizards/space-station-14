using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForensicsComponent : Component
{
    /// <summary>
    /// The fingerprint strings.
    /// </summary>
    [DataField]
    public HashSet<string> Fingerprints = [];

    /// <summary>
    /// The fiber strings from stuff like gloves.
    /// </summary>
    [DataField]
    public HashSet<string> Fibers = [];

    /// <summary>
    /// The DNA strings from blood or when someone is struck by something.
    /// </summary>
    [DataField]
    public HashSet<string> DNAs = [];

    /// <summary>
    /// Residues like soap when cleaning an item.
    /// </summary>
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
