namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed partial class IgniteOnMeleeHitComponent : Component
{
    [DataField("fireStacks")]
    public float FireStacks { get; set; }
}
