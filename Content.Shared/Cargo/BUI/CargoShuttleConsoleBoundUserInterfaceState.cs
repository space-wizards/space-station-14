using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.BUI;

[Serializable, NetSerializable]
public sealed class CargoShuttleConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public string AccountName;
    public string ShuttleName;

    // Unfortunately shuttles have essentially 3 states so can't just use a nullable var for it:
    // 1. stowed
    // 2. called but not recallable
    // 3. called and recallable
    // The reason we have 2 is so people don't spam the recall button in the UI.
    public bool CanRecall;

    /// <summary>
    /// When the shuttle is expected to be usable.
    /// </summary>
    public TimeSpan? ShuttleETA;

    /// <summary>
    /// List of orders expected on the delivery.
    /// </summary>
    public List<CargoOrderData> Orders;

    public CargoShuttleConsoleBoundUserInterfaceState(
        string accountName,
        string shuttleName,
        bool canRecall,
        TimeSpan? shuttleETA,
        List<CargoOrderData> orders)
    {
        AccountName = accountName;
        ShuttleName = shuttleName;
        CanRecall = canRecall;
        ShuttleETA = shuttleETA;
        Orders = orders;
    }
}
