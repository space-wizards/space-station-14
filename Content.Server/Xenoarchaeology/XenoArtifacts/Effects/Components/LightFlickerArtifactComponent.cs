namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Flickers all the lights within a certain radius.
/// </summary>
[RegisterComponent]
public sealed partial class LightFlickerArtifactComponent : Component
{
    /// <summary>
    /// Lights within this radius will be flickered on activation
    /// </summary>
    [DataField("radius")]
    public float Radius = 4;

    /// <summary>
    /// The chance that the light will flicker
    /// </summary>
    [DataField("flickerChance")]
    public float FlickerChance = 0.75f;
}
