namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed partial class TritiumRadiationSourceComponent : Component
{
    [DataField("lifetime")]
    public float Lifetime = 2f;
}
