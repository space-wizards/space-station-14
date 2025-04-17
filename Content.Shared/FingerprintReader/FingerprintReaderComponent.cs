using Robust.Shared.GameStates;

namespace Content.Shared.FingerprintReader;

/// <summary>
/// Component for checking if a user's fingerprint matches allowed fingerprints
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(FingerprintReaderSystem))]
public sealed partial class FingerprintReaderComponent : Component
{
    /// <summary>
    /// The fingerprints that are allowed to access this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> AllowedFingerprints = new();

    /// <summary>
    /// Whether to ignore gloves when checking fingerprints.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreGloves;

    /// <summary>
    /// The popup to show when access is denied due to fingerprint mismatch.
    /// </summary>
    [DataField]
    public LocId? FailPopup;

    /// <summary>
    /// The popup to show when access is denied due to wearing gloves.
    /// </summary>
    [DataField]
    public LocId? FailGlovesPopup;
}
