namespace Content.Server.Instruments;

[RegisterComponent]
public sealed partial class SwappableInstrumentComponent : Component
{
    /// <summary>
    /// Used to store the different instruments that can be swapped between.
    /// string = display name of the instrument
    /// byte 1 = instrument midi program
    /// byte 2 = instrument midi bank
    /// </summary>
    [DataField("instrumentList", required: true)]
    public Dictionary<string, (byte, byte)> InstrumentList = new();
}
