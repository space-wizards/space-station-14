using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.GameTicking.Events;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.GameTicking;
using Robust.Server.Maps;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    /*
     * Handles cargo shuttle mechanics, including cargo shuttle consoles.
     */

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapLoader _loader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;

    public MapId? CargoMap { get; private set; }

    private const float ShuttleRecallRange = 100f;

    private const float CallOffset = 50f;

    private int _index;

    private void InitializeShuttle()
    {
        SubscribeLocalEvent<CargoShuttleComponent, MoveEvent>(OnCargoShuttleMove);
        SubscribeLocalEvent<CargoShuttleConsoleComponent, ComponentStartup>(OnCargoShuttleConsoleStartup);
        SubscribeLocalEvent<CargoShuttleConsoleComponent, CargoCallShuttleMessage>(OnCargoShuttleCall);
        SubscribeLocalEvent<CargoShuttleConsoleComponent, CargoRecallShuttleMessage>(RecallCargoShuttle);
        SubscribeLocalEvent<StationCargoOrderDatabaseComponent, ComponentStartup>(OnCargoOrderStartup);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
    }

    #region Console

    private void UpdateShuttleCargoConsoles(CargoShuttleComponent component)
    {
        foreach (var console in EntityQuery<CargoShuttleConsoleComponent>(true))
        {
            var stationUid = _station.GetOwningStation(console.Owner);
            if (stationUid != component.Station) continue;
            UpdateShuttleState(console, stationUid);
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
                CanRecallShuttle(shuttle?.Owner, out _),
                shuttle?.NextCall,
                orders));
    }

    #endregion

    #region Shuttle

    private void OnCargoShuttleMove(EntityUid uid, CargoShuttleComponent component, ref MoveEvent args)
    {
        if (component.Station == null) return;

        var oldCanRecall = component.CanRecall;

        // Check if we can update the recall status.
        var canRecall = CanRecallShuttle(uid, out _, args.Component);
        if (oldCanRecall == canRecall) return;

        component.CanRecall = canRecall;
        _sawmill.Debug($"Updated CanRecall for {ToPrettyString(uid)}");
        UpdateShuttleCargoConsoles(component);
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
        var space = GetCargoPallets(component).Count;
        return space;
    }

    private List<CargoPalletComponent> GetCargoPallets(CargoShuttleComponent component)
    {
        var pads = new List<CargoPalletComponent>();

        foreach (var (comp, compXform) in EntityQuery<CargoPalletComponent, TransformComponent>(true))
        {
            if (compXform.ParentUid != component.Owner ||
                !compXform.Anchored) continue;

            pads.Add(comp);
        }

        return pads;
    }

    #endregion

    #region Station

    private void OnCargoOrderStartup(EntityUid uid, StationCargoOrderDatabaseComponent component, ComponentStartup args)
    {
        // Stations get created first but if any are added at runtime then do this.
        AddShuttle(component);
    }

    private void AddShuttle(StationCargoOrderDatabaseComponent component)
    {
        if (CargoMap == null || component.Shuttle != null) return;

        if (component.CargoShuttleProto != null)
        {
            var prototype = _protoMan.Index<CargoShuttlePrototype>(component.CargoShuttleProto);

            var (_, gridId) = _loader.LoadBlueprint(CargoMap.Value, prototype.Path.ToString());
            var shuttleUid = _mapManager.GetGridEuid(gridId!.Value);
            var xform = Transform(shuttleUid);

            // TODO: Something better like a bounds check.
            xform.LocalPosition += 100 * _index;
            var comp = EnsureComp<CargoShuttleComponent>(shuttleUid);
            comp.Station = component.Owner;
            comp.Coordinates = xform.Coordinates;

            component.Shuttle = shuttleUid;
            comp.NextCall = _timing.CurTime + TimeSpan.FromSeconds(comp.Cooldown);
            UpdateShuttleCargoConsoles(comp);
            _index++;
            _sawmill.Info($"Added cargo shuttle to {ToPrettyString(shuttleUid)}");
        }
    }

    private void SellPallets(CargoShuttleComponent component, StationBankAccountComponent bank)
    {
        double amount = 0;
        var toSell = new HashSet<EntityUid>();

        foreach (var pallet in GetCargoPallets(component))
        {
            // Containers should already get the sell price of their children so can skip those.
            foreach (var ent in _lookup.GetEntitiesIntersecting(pallet.Owner, LookupFlags.Anchored))
            {
                if (toSell.Contains(ent)) continue;
                var price = _pricing.GetPrice(ent);
                if (price == 0) continue;
                amount += price;
            }
        }

        bank.Balance += (int) amount;

        foreach (var ent in toSell)
        {
            Del(ent);
        }
    }

    private void SendToCargoMap(EntityUid uid, CargoShuttleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.NextCall = _timing.CurTime + TimeSpan.FromSeconds(component.Cooldown);
        Transform(uid).Coordinates = component.Coordinates;
        DebugTools.Assert(MetaData(uid).EntityPaused);

        UpdateShuttleCargoConsoles(component);
        _sawmill.Info($"Stashed cargo shuttle {ToPrettyString(uid)} from {ToPrettyString(uid)}");
    }

    /// <summary>
    /// Retrieves a shuttle for delivery.
    /// </summary>
    public void CallShuttle(StationCargoOrderDatabaseComponent orderDatabase)
    {
        if (!TryComp<CargoShuttleComponent>(orderDatabase.Shuttle, out var shuttle) ||
            !TryComp<TransformComponent>(orderDatabase.Owner, out var xform)) return;

        // Already called / not available (TODO: Send message)
        if (shuttle.NextCall == null || _timing.CurTime < shuttle.NextCall)
            return;

        shuttle.NextCall = null;

        // Find a valid free area nearby to spawn in on
        var center = new Vector2();
        var minRadius = 0f;
        Box2? aabb = null;

        foreach (var grid in _mapManager.GetAllMapGrids(xform.MapID))
        {
            aabb = aabb?.Union(grid.WorldAABB) ?? grid.WorldAABB;
        }

        if (aabb != null)
        {
            center = aabb.Value.Center;
            minRadius = MathF.Max(aabb.Value.Width, aabb.Value.Height);
        }

        Transform(shuttle.Owner).Coordinates = new EntityCoordinates(xform.ParentUid,
            center + _random.NextVector2(minRadius + CallOffset, minRadius + ShuttleRecallRange));
        DebugTools.Assert(!MetaData(shuttle.Owner).EntityPaused);

        AddCargoContents(shuttle, orderDatabase);

        // Update consoles
        foreach (var comp in EntityQuery<CargoShuttleConsoleComponent>(true))
        {
            var station = _station.GetOwningStation(comp.Owner);
            if (station != orderDatabase.Owner) continue;
            UpdateShuttleState(comp);
        }

        _sawmill.Info($"Retrieved cargo shuttle {ToPrettyString(shuttle.Owner)} from {ToPrettyString(orderDatabase.Owner)}");
    }

    private void AddCargoContents(CargoShuttleComponent component, StationCargoOrderDatabaseComponent orderDatabase)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var orders = GetProjectedOrders(orderDatabase, component);

        var pads = GetCargoPallets(component);
        DebugTools.Assert(orders.Sum(o => o.Amount) <= pads.Count);

        for (var i = 0; i < orders.Count; i++)
        {
            var order = orders[i];

            Spawn(_protoMan.Index<CargoProductPrototype>(order.ProductId).Product,
                new EntityCoordinates(component.Owner, xformQuery.GetComponent(_random.PickAndTake(pads).Owner).LocalPosition));
            order.Amount--;

            if (order.Amount == 0)
            {
                orders.RemoveSwap(i);
                orderDatabase.Orders.Remove(order.OrderNumber);
                i--;
            }
            else
            {
                orderDatabase.Orders[order.OrderNumber] = order;
            }
        }
    }

    public bool CanRecallShuttle(EntityUid? uid, [NotNullWhen(false)] out string? reason, TransformComponent? xform = null)
    {
        reason = null;

        if (!TryComp<IMapGridComponent>(uid, out var grid) ||
            !Resolve(uid.Value, ref xform)) return true;

        var bounds = grid.Grid.WorldAABB.Enlarged(ShuttleRecallRange);

        foreach (var other in _mapManager.FindGridsIntersecting(xform.MapID, bounds))
        {
            if (grid.GridIndex == other.Index) continue;
            reason = $"Too close to nearby objects";
            return false;
        }

        return true;
    }

    private void RecallCargoShuttle(EntityUid uid, CargoShuttleConsoleComponent component, CargoRecallShuttleMessage args)
    {
        var stationUid = _station.GetOwningStation(component.Owner);

        if (!TryComp<StationCargoOrderDatabaseComponent>(stationUid, out var orderDatabase) ||
            !TryComp<StationBankAccountComponent>(stationUid, out var bank)) return;

        if (!TryComp<CargoShuttleComponent>(orderDatabase.Shuttle, out var shuttle))
        {
            _popup.PopupEntity($"No cargo shuttle found!", args.Entity, Filter.Entities(args.Entity));
            return;
        }

        if (!CanRecallShuttle(shuttle.Owner, out var reason))
        {
            _popup.PopupEntity(reason, args.Entity, Filter.Entities(args.Entity));
            return;
        }

        SellPallets(shuttle, bank);

        SendToCargoMap(orderDatabase.Shuttle.Value);
    }

    private void OnCargoShuttleCall(EntityUid uid, CargoShuttleConsoleComponent component, CargoCallShuttleMessage args)
    {
        var stationUid = _station.GetOwningStation(args.Entity);
        if (!TryComp<StationCargoOrderDatabaseComponent>(stationUid, out var orderDatabase)) return;
        CallShuttle(orderDatabase);
    }

    #endregion

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
}
