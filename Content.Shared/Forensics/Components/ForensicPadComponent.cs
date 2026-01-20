using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

/// <summary>
/// Used to take a sample of someone's fingerprints.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForensicPadComponent : Component
{
    /// <summary>
    /// The amount of time for the pad to be used to take a sample.
    /// </summary>
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(3.0f);

    [AutoNetworkedField]
    public bool Used;

    [AutoNetworkedField]
    public String Sample = string.Empty;
}
