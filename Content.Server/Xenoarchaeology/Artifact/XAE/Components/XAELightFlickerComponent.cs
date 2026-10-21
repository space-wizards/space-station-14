namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Flickers all the lights within a certain radius.
/// </summary>
[RegisterComponent, Access(typeof(XAELightFlickerSystem))]
public sealed partial class XAELightFlickerComponent : Component
{
    /// <summary>
    /// Lights within this radius will be flickered on activation.
    /// </summary>
    [DataField]
    public float Radius = 4;

    /// <summary>
    /// The chance that the light will flicker.
    /// </summary>
    [DataField]
    public float FlickerChance = 0.75f;
}
