using Content.Shared._DeltaV.Shipyard.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._DeltaV.Shipyard;

[Serializable, NetSerializable]
public enum ShipyardConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ShipyardConsoleState : BoundUserInterfaceState
{
    public readonly int Balance;

    public ShipyardConsoleState(int balance)
    {
        Balance = balance;
    }
}

/// <summary>
/// Ask the server to purchase a vessel.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShipyardConsolePurchaseMessage : BoundUserInterfaceMessage
{
    public readonly ProtoId<VesselPrototype> Vessel;

    public ShipyardConsolePurchaseMessage(string vessel)
    {
        Vessel = vessel;
    }
}
