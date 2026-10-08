using System.Linq;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Cargo.Components;

/// <summary>
/// Stores all of cargo orders for a particular station.
/// </summary>
[RegisterComponent]
public sealed partial class StationCargoOrderDatabaseComponent : Component
{
    /// <summary>
    /// Maximum amount of orders a station is allowed, approved or not.
    /// </summary>
    [DataField]
    public int Capacity = 20;

    /// <summary>
    /// Every outstanding order across every account.
    /// </summary>
    [ViewVariables]
    public IEnumerable<CargoOrderData> AllOrders => Orders.SelectMany(p => p.Value);

    /// <summary>
    /// A dictionary containing every outstanding order on the system, indexed by account.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CargoAccountPrototype>, List<CargoOrderData>> Orders = new();

    /// <summary>
    /// Used to determine unique order IDs.
    /// </summary>
    [ViewVariables]
    public int NumOrdersCreated;

    /// <summary>
    /// An all encompassing determiner of what markets can be ordered from.
    /// Not every console can order from every market, but a console can't order from a market not on this list.
    /// </summary>
    [DataField]
    public List<ProtoId<CargoMarketPrototype>> Markets = new()
    {
        "market",
    };

    // TODO: Can probably dump this
    /// <summary>
    /// The cargo shuttle assigned to this station.
    /// </summary>
    [DataField("shuttle")]
    public EntityUid? Shuttle;

    /// <summary>
    /// The paper-type prototype to spawn with the order information.
    /// </summary>
    [DataField]
    public EntProtoId PrinterOutput = "PaperCargoInvoice";
}

/// <summary>
/// Event broadcast before a cargo order is fulfilled, allowing alternate systems to fulfill the order.
/// </summary>
[ByRefEvent]
public record struct FulfillCargoOrderEvent(Entity<StationDataComponent> Station, CargoOrderData Order)
{
    /// <summary>
    /// The station that placed the order.
    /// </summary>
    public readonly Entity<StationDataComponent> Station = Station;

    /// <summary>
    /// The <see cref="CargoOrderData"/> representing the order.
    /// </summary>
    public readonly CargoOrderData Order = Order;

    /// <summary>
    /// The entity that is fulfilling the order, e.g. the telepad where an order will arrive.
    /// </summary>
    public EntityUid? FulfillmentEntity;

    /// <summary>
    /// If this event has already been handled.
    /// </summary>
    public bool Handled = false;
}
