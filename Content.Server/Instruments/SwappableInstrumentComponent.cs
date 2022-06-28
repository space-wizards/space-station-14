namespace Content.Server.Instruments;

[RegisterComponent]
public sealed class SwappableInstrumentComponent : Component
{
    /// <summary>
    /// string  = the name of the style, used for display
    /// byte    = the corresponding program number for the instrument
    /// </summary>
    [DataField("instrumentList", required: true)]
    public Dictionary<string, byte> InstrumentList = new();
}
