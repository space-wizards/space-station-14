namespace Content.Server.Medical.Components;

[RegisterComponent]
public sealed class InsideCryoPodComponent: Component
{
    [ViewVariables]
    public EntityUid Holder;

    /// <summary>
    /// Cold damage threshold override
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("coldThreshold")]
    public int ColdThreshold = 10;
}
