namespace Content.Client.Fax;

[RegisterComponent]
public sealed class FaxMachineVisualsComponent : Component
{
    [DataField("normalState", required: true)]
    public string NormalState = default!;

    [DataField("insertingState", required: true)]
    public string InsertingState = default!;
    
    [DataField("printState", required: true)]
    public string PrintState = default!;
}

public enum FaxMachineVisualLayers : byte
{
    Base,
}
