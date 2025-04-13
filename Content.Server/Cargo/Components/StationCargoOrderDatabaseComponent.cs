using System.Linq;
using Content.Server.Station.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
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

    [ViewVariables]
    public IEnumerable<CargoOrderData> AllOrders => Orders.SelectMany(p => p.Value);

    [DataField]
    public Dictionary<ProtoId<CargoAccountPrototype>, List<CargoOrderData>> Orders = new();

    /// <summary>
    /// Used to determine unique order IDs
    /// </summary>
    [ViewVariables]
    public int NumOrdersCreated;

    // TODO: Can probably dump this
    /// <summary>
    /// The cargo shuttle assigned to this station.
    /// </summary>
    [DataField("shuttle")]
    public EntityUid? Shuttle;

    /// <summary>
    ///     The paper-type prototype to spawn with the order information.
    /// </summary>
    [DataField]
    public EntProtoId PrinterOutput = "PaperCargoInvoice";
}

/// <summary>
/// Event broadcast before a cargo order is fulfilled, allowing alternate systems to fulfill the order.
/// </summary>
[ByRefEvent]
public record struct FulfillCargoOrderEvent(Entity<StationDataComponent> Station, CargoOrderData Order, Entity<CargoOrderConsoleComponent> OrderConsole)
{
    public Entity<CargoOrderConsoleComponent> OrderConsole = OrderConsole;
    public Entity<StationDataComponent> Station = Station;
    public CargoOrderData Order = Order;

    public EntityUid? FulfillmentEntity;
    public bool Handled = false;
}
