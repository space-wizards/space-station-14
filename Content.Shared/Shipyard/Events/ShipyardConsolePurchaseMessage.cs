using Robust.Shared.Serialization;

namespace Content.Shared.Shipyard.Events;

/// <summary>
///     Purchase a Vessel from the console
/// </summary>
[Serializable, NetSerializable]
public sealed class ShipyardConsolePurchaseMessage : BoundUserInterfaceMessage
{
    public string Vessel; //vessel prototype ID

    public ShipyardConsolePurchaseMessage(string vessel)
    {
        Vessel = vessel;
    }
}
