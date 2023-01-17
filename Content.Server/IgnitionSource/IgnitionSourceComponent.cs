namespace Content.Server.IgnitionSource;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
[Access(typeof(IgnitionSourceSystem))]
public sealed class IgnitionSourceComponent : Component
{
    [DataField("state")]
    public bool State = false;

    [DataField("temperature")]
    public int emperature;
}
