namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed class IgniteOnMeleeHitComponent : Component
{
    [DataField("fireStacks")]
    public float FireStacks { get; set; }
}
