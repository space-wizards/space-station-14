// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.BUI;

[Serializable, NetSerializable]
public sealed class CargoShuttleConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public string AccountName;
    public string ShuttleName;

    /// <summary>
    /// List of orders expected on the delivery.
    /// </summary>
    public List<CargoOrderData> Orders;

    public CargoShuttleConsoleBoundUserInterfaceState(
        string accountName,
        string shuttleName,
        List<CargoOrderData> orders)
    {
        AccountName = accountName;
        ShuttleName = shuttleName;
        Orders = orders;
    }
}
