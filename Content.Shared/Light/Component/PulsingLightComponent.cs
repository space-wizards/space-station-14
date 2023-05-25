using Robust.Shared.GameStates;

namespace Content.Shared.Light.Component;

/// <summary>
/// This is used for lights that increase in brightness/radius in a cyclical manner
/// Modeled with a cosine function.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedPulsingLightSystem)), AutoGenerateComponentState]
public sealed partial class PulsingLightComponent : Robust.Shared.GameObjects.Component
{
    /// <summary>
    /// Whether or not the pulsing effect is enabled.
    /// Setting this to false simply halts the pulse, but does not reset the value.
    /// </summary>
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The minimum brightness of the light
    /// </summary>
    [DataField("minBrightness"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float MinBrightness;

    /// <summary>
    /// The maximum brightness of the light
    /// </summary>
    [DataField("maxBrightness"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float MaxBrightness;

    /// <summary>
    /// The minimum brightness of the light
    /// </summary>
    [DataField("minRadius"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float MinRadius;

    /// <summary>
    /// The maximum brightness of the light
    /// </summary>
    [DataField("maxRadius"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float MaxRadius;

    /// <summary>
    /// How long does it take the light to complete one "cycle"
    /// Value is in seconds.
    /// </summary>
    [DataField("period"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Period;

    /// <summary>
    /// Whether or not the value of the pulse should be randomly offset
    /// </summary>
    [DataField("randomlyOffset")]
    public bool RandomlyOffset = true;

    /// <summary>
    /// The random offset of the pulse
    /// </summary>
    [DataField("randomOffset"), ViewVariables(VVAccess.ReadWrite)]
    public float RandomOffset;
}
