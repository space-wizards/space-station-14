namespace Content.Server._Tinystation.Speech.Components;

[RegisterComponent]
public sealed partial class KnightAccentComponent : Component
{
    [DataField("ackChance")]
    public float ackChance = 0.3f;
}
