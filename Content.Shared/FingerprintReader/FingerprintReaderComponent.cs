using Robust.Shared.GameStates;

namespace Content.Shared.FingerprintReader;

/// <summary>
/// Component for checking if a user's fingerprint matches allowed fingerprints
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FingerprintReaderComponent : Component
{
    /// <summary>
    /// Whether the fingerprint reader is enabled
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The fingerprints that are allowed to access this entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> AllowedFingerprints = new();

    /// <summary>
    /// Whether to ignore gloves when checking fingerprints
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreGloves;

    /// <summary>
    /// The popup to show when access is denied due to fingerprint mismatch
    /// </summary>
    [DataField]
    public LocId? FailPopup;

    /// <summary>
    /// The popup to show when access is denied due to wearing gloves
    /// </summary>
    [DataField]
    public LocId? FailGlovesPopup;
}

/// <summary>
/// Event raised when a fingerprint reader's configuration changes
/// </summary>
[ByRefEvent]
public readonly record struct FingerprintReaderConfigurationChangedEvent;
