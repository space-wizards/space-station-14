using Robust.Shared.GameStates;

namespace Content.Shared.Screech;

/// <summary>
/// This component displays & configures screen-distorting screeches. The associated overlay is <see cref="ScreechShockWaveOverlay"/> on the client side.
/// This component by itself has no stunning propriety; it is solely for the display of screeches.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class ScreechShockWaveComponent : Component
{
    /// <summary>
    ///   The speed of each individual wave from the center axis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WaveSpeed = 15.3f;

    /// <summary>
    ///     The size of each wave in its width and distortion effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WaveStrength = 1.0f;

    /// <summary>
    ///     The scale of the effect, lower number means a larger total area while smaller numbers downscale it and reduce the effected area.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DownScale = 1.5f;

    /// <summary>
    ///     The time it takes for the effect to completely fade out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FadeTime = 3.0f;

    /// <summary>
    /// Used with FadeTime to properly fade out the effect.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan InitTime;
}
