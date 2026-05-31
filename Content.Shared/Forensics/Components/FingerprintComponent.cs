namespace Content.Shared.Forensics.Components;

/// <summary>
/// This component is for mobs that leave fingerprints.
/// </summary>
[RegisterComponent]
public sealed partial class FingerprintComponent : Component
{
    [DataField]
    public string? Fingerprint;
}
