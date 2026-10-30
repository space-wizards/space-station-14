using Robust.Shared.GameStates;

namespace Content.Shared.Instruments;

[RegisterComponent, NetworkedComponent]
public sealed partial class SwappableInstrumentComponent : Component
{
    /// <summary>
    /// Used to store the different instruments that can be swapped between.
    /// LocId = localization key for the display name of the instrument
    /// byte 1 = instrument midi program
    /// byte 2 = instrument midi bank
    /// </summary>
    [DataField(required: true)]
    public Dictionary<LocId, (byte, byte)> InstrumentList = [];
}
