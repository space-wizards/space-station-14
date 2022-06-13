using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.GameTicking.Events;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.GameTicking;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    [Dependency] private readonly IMapLoader _loader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public MapId? CargoMap { get; private set; }

    // TODO: Store this on the component ya mong
    // ZTODO: Store cargo shuttle on station comp.

    private int _index;

    #region Setup

    private void InitializeShuttle()
    {
        SubscribeLocalEvent<CargoShuttleConsoleComponent, ComponentStartup>(OnCargoShuttleConsoleStartup);
        SubscribeLocalEvent<StationCargoOrderDatabaseComponent, ComponentStartup>(OnCargoOrderStartup);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
    }

    private void OnCargoOrderStartup(EntityUid uid, StationCargoOrderDatabaseComponent component, ComponentStartup args)
    {
        // Stations get created first but if any are added at runtime then do this.
        AddShuttle(component);
    }

    private void AddShuttle(StationCargoOrderDatabaseComponent component)
    {
        if (CargoMap == null || component.Shuttle != null) return;

        if (component.CargoShuttleProto != null &&
            _protoMan.TryIndex<CargoShuttlePrototype>(component.CargoShuttleProto, out var prototype))
        {
            var (_, gridId) = _loader.LoadBlueprint(CargoMap.Value, prototype.Path.ToString());
            var shuttleUid = _mapManager.GetGridEuid(gridId!.Value);
            var xform = Transform(shuttleUid);

            // TODO: Something better like a bounds check.
            xform.LocalPosition += 100 * _index;
            var comp = EnsureComp<CargoShuttleComponent>(shuttleUid);
            comp.Station = component.Owner;
            comp.Coordinates = xform.Coordinates;

            component.Shuttle = shuttleUid;
            _index++;
            _sawmill.Info($"Added cargo shuttle to {ToPrettyString(shuttleUid)}");
        }
    }

    private void OnCargoShuttleConsoleStartup(EntityUid uid, CargoShuttleConsoleComponent component, ComponentStartup args)
    {
        var station = _station.GetOwningStation(uid);

        UpdateShuttleState(component, station);
    }

    private void UpdateShuttleState(CargoShuttleConsoleComponent component, EntityUid? station = null)
    {
        TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase);
        TryComp<CargoShuttleComponent>(orderDatabase?.Shuttle, out var shuttle);

        var orders = GetProjectedOrders(orderDatabase, shuttle);

        // TODO: Loc
        _uiSystem.GetUiOrNull(component.Owner, CargoConsoleUiKey.Shuttle)?.SetState(
            new CargoShuttleConsoleBoundUserInterfaceState(
                station != null ? MetaData(station.Value).EntityName : "Unknown",
                orderDatabase?.Shuttle != null ? MetaData(orderDatabase.Shuttle.Value).EntityName : "Not found",
                TimeSpan.Zero,
                orders));
    }

    /// <summary>
    /// Returns the orders that can fit on the cargo shuttle.
    /// </summary>
    private List<CargoOrderData> GetProjectedOrders(
        StationCargoOrderDatabaseComponent? component = null,
        CargoShuttleComponent? shuttle = null)
    {
        var orders = new List<CargoOrderData>();

        if (component == null || shuttle == null || component.Orders.Count == 0)
            return orders;

        var space = GetCargoSpace(shuttle);

        if (space == 0) return orders;

        var indices = component.Orders.Keys.ToList();
        indices.Sort();
        var amount = 0;

        foreach (var index in indices)
        {
            var order = component.Orders[index];
            if (!order.Approved) continue;

            var cappedAmount = Math.Min(space - amount, order.Amount);
            amount += cappedAmount;
            DebugTools.Assert(amount <= space);

            if (cappedAmount < order.Amount)
            {
                var reducedOrder = new CargoOrderData(order.OrderNumber, order.Requester, order.Reason, order.ProductId,
                    cappedAmount);

                orders.Add(reducedOrder);
                break;
            }

            orders.Add(order);

            if (amount == space) break;
        }

        return orders;
    }

    /// <summary>
    /// Get the amount of space the cargo shuttle can fit for orders.
    /// </summary>
    private int GetCargoSpace(CargoShuttleComponent component)
    {
        return 1;
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        Setup();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (CargoMap == null)
        {
            DebugTools.Assert(!EntityQuery<CargoShuttleComponent>().Any());
            return;
        }

        _mapManager.DeleteMap(CargoMap.Value);
        CargoMap = null;

        // Shuttle may not have been in the cargo dimension (e.g. on the station map) so need to delete.
        foreach (var comp in EntityQuery<CargoShuttleComponent>())
        {
            QueueDel(comp.Owner);
        }
    }

    private void Setup()
    {
        if (CargoMap != null)
        {
            _sawmill.Error($"Tried to setup cargo dimension when it's already setup!");
            return;
        }

        // It gets mapinit which is okay... buuutt we still want it paused to avoid power draining.
        CargoMap = _mapManager.CreateMap();
        _mapManager.SetMapPaused(CargoMap!.Value, true);

        foreach (var comp in EntityQuery<StationCargoOrderDatabaseComponent>(true))
        {
            AddShuttle(comp);
        }
    }

    #endregion

    public int GetCargoSlots(EntityUid uid)
    {
        // TODO: Need to get the uhh cargo pad idea thing.
        return 0;
    }

    private int GetPrice(EntityUid uid, int price = 0)
    {
        // TODO: Use cargo pads.
        var xform = Transform(uid);
        var childEnumerator = xform.ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            price += GetPrice(child.Value);
        }

        return price;
    }

    public void SendToCargoDimension(EntityUid uid, CargoShuttleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        Transform(uid).Coordinates = component.Coordinates;
        DebugTools.Assert(MetaData(uid).EntityPaused);
    }

    /// <summary>
    /// Retrieves a shuttle for delivery.
    /// </summary>
    public void RetrieveFromCargoDimension(EntityUid uid, EntityUid gridUid)
    {

    }

    public bool CanSendToCargoDimension(EntityUid uid)
    {
        return true;
    }
}

[Prototype("cargoShuttle")]
public sealed class CargoShuttlePrototype : IPrototype
{
    [ViewVariables]
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [ViewVariables, DataField("path")]
    public ResourcePath Path = default!;
}
