namespace Content.Client.Radio;

[RegisterComponent]
public sealed class TelecommsMachineVisualsComponent : Component
{
    [DataField("onState", required: true)] public string OnState = default!;

    [DataField("offState", required: true)]
    public string OffState = default!;

    /// <summary>
    /// Icon state to use when this transmits/receives. Optional
    /// </summary>
    [DataField("txrxState")] public string? TXRXState;
}
