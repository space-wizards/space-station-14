/// <summary>
///     Irradiate all
/// </summary>
[RegisterComponent]
public sealed class RadiationSourceComponent : Component
{
    [ViewVariables]
    [DataField("radsPerSecond")]
    public float RadsPerSecond = 1;

    [ViewVariables]
    [DataField("range")]
    public float Range = 5f;
}
