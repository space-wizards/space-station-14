using Robust.Shared.Serialization;

namespace Content.Shared.Shipyard.BUI;

[NetSerializable, Serializable]
public sealed class ShipyardConsoleInterfaceState : BoundUserInterfaceState
{
    public int Balance;
    public readonly bool AccessGranted;

    public ShipyardConsoleInterfaceState(
        int balance,
        bool accessGranted)
    {
        Balance = balance;
        AccessGranted = accessGranted;
    }
}
