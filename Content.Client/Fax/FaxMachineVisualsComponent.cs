namespace Content.Client.Fax;

[RegisterComponent]
public sealed class FaxMachineVisualsComponent : Component
{
    [DataField("receivingState", required: true)]
    public string ReceivingState = default!;

    [DataField("sendingState", required: true)]
    public string SendingState = default!;
}

public enum FaxMachineVisualLayers : byte
{
    IsReceiving,
    IsSending
}
