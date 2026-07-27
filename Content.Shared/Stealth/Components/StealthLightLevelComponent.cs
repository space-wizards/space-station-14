using Robust.Shared.GameStates;

namespace Content.Shared.Stealth.Components;

/// <summary>
/// When added to an entity with stealth component, this component will change the visibility
/// based on the light level.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedStealthSystem))]
public sealed partial class StealthInDarkComponent : Component
{
    /// <summary>
    /// If the light level is lower, stealth is enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ActivatedLightLevel;

    /// <summary>
    /// The minimum visibility value that this component can set
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DarkVisibilityRate = -0.01f;

    /// <summary>
    /// The minimum visibility value that this component can set
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LightVisibilityRate = 0.01f;

    [DataField, AutoNetworkedField]
    public float ChangedVisibility;
}
