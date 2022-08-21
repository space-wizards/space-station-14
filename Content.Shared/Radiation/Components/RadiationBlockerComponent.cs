namespace Content.Shared.Radiation.Components;

[RegisterComponent]
public sealed class RadiationBlockerComponent : Component
{
    [DataField("enabled")]
    public bool Enabled = true;

    [DataField("resistance")]
    public float RadResistance = 1f;

    public (EntityUid Grid, Vector2i Tile)? LastPosition;
}
