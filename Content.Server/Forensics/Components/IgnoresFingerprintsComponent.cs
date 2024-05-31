namespace Content.Server.Forensics.Components;

/// <summary>
/// This component is for entities we do not wish to track fingerprints/fibers, like puddles
/// </summary>
[RegisterComponent]
public sealed partial class IgnoresFingerprintsComponent : Component { }
