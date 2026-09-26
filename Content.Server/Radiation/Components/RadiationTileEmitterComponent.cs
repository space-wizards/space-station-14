namespace Content.Server.Radiation.Components;

[RegisterComponent]
public sealed partial class TileRadiationEmitterComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public float Intensity = 2f;

    [DataField]
    public float Slope = 0.5f;

    // Set to 0 for infinite lifespan. Set to -1 for single tick pulses.
    [DataField]
    public float HalfLife = -1f;
}
