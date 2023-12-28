namespace Content.Server.IgnitionSource;

/// <summary>
/// This is used for creating atmosphere hotspots while ignited to start reactions such as fire.
/// </summary>
[RegisterComponent, Access(typeof(IgnitionSourceSystem))]
public sealed partial class IgnitionSourceComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Ignited;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public int Temperature;
}
