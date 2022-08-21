namespace Content.Shared.Radiation.Components;

[RegisterComponent]
public sealed class RadiationBlockerComponent : Component
{
    [DataField("resistance")]
    public float RadResistance = 1f;
}
