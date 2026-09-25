namespace Content.Server.Radiation.Components;

[RegisterComponent]
public sealed partial class TileRadiationEmitterComponent : Component
{
    [DataField("enabled")]
    public bool Enabled = true;

    [DataField("intensity")]
    public float Intensity = 2f;

    [DataField("slope")]
    public float Slope = 0.5f;

    // Set to 0 for infinite lifespan. Set to -1 for single tick pulses.
    [DataField("halfLife")]
    public float HalfLife = -1f;
}
