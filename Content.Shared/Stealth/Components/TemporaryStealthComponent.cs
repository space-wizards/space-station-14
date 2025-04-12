using Robust.Shared.GameStates;

namespace Content.Shared.Stealth.Components;

/// <summary>
/// Some systems can make an item temporarily invisible.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TemporaryStealthComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan FadeInTime = TimeSpan.FromSeconds(2f);

    [DataField, AutoNetworkedField]
    public TimeSpan FadeOutTime = TimeSpan.FromSeconds(3f);

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public float TargetVisibility = -1f;

    [DataField, AutoNetworkedField]
    public TimeSpan StartTime = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public bool RemoveStealth = false;
}
