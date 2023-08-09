namespace Content.Server.IgnitionSource;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
[Access(typeof(IgnitionSourceSystem))]
public sealed class IgnitionSourceComponent : Component
{
    [DataField("ignited")]
    public bool Ignited = false;

    [DataField("temperature", required: true)]
    public int Temperature;
}
