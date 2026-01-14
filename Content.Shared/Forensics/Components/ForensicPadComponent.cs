using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

/// <summary>
/// Used to take a sample of someone's fingerprints.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForensicPadComponent : Component
{
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(3);

    [AutoNetworkedField]
    public bool Used = false;

    [AutoNetworkedField]
    public string Sample = string.Empty;
}
