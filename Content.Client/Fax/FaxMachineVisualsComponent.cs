namespace Content.Client.Fax;

[RegisterComponent]
public sealed class FaxMachineVisualsComponent : Component
{
    [DataField("offState", required: true)]
    public string OffState = default!;

    [DataField("idleState", required: true)]
    public string IdleState = default!;

    [DataField("insertingState", required: true)]
    public string InsertingState = default!;
    
    [DataField("printState", required: true)]
    public string PrintState = default!;
}

public enum FaxMachineVisualLayers : byte
{
    Base,
}
