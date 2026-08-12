using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Stealth.Components;

/// <summary>
/// When added to an entity with stealth component, this component will change the visibility
/// based on the light level.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedStealthSystem))]
public sealed partial class StealthInDarkComponent : Component
{
    /// <summary>
    /// If the light level is lower, stealth is enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ActivatedLightLevel;

    /// <summary>
    /// Visibility changes after a certain amount of time when player is in the dark.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DarkVisibilityRate = -0.01f;

    /// <summary>
    /// Visibility changes after a certain amount of time when player is in the light.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LightVisibilityRate = 0.01f;

    /// <summary>
    /// Delay between changes in visibility.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Interval = TimeSpan.FromSeconds(0.1);

    /// <summary>
    /// Next time visibility changes
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables, AutoPausedField, AutoNetworkedField]
    public TimeSpan NextVisibilityChange;

    /// <summary>
    /// How much visibility has already been changed using this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ChangedVisibility;
}
