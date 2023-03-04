using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Labels.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.UserInterface;
using Content.Server.Paper;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.GameTicking;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    /*
     * Handles cargo shuttle mechanics, including cargo shuttle consoles.
     */

    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;

    public MapId? CargoMap { get; private set; }

    private int _index;

    /// <summary>
    /// Whether cargo shuttles are enabled at all. Mainly used to disable cargo shuttle loading for performance reasons locally.
    /// </summary>
    private bool _enabled;

    private void InitializeShuttle()
    {
        _enabled = _configManager.GetCVar(CCVars.CargoShuttles);
        // Don't want to immediately call this as shuttles will get setup in the natural course of things.
        _configManager.OnValueChanged(CCVars.CargoShuttles, SetCargoShuttleEnabled);

        SubscribeLocalEvent<CargoShuttleComponent, FTLStartedEvent>(OnCargoFTLStarted);
        SubscribeLocalEvent<CargoShuttleComponent, FTLCompletedEvent>(OnCargoFTLCompleted);
        SubscribeLocalEvent<CargoShuttleComponent, FTLTagEvent>(OnCargoFTLTag);

        SubscribeLocalEvent<CargoShuttleConsoleComponent, ComponentStartup>(OnCargoShuttleConsoleStartup);

        SubscribeLocalEvent<CargoPilotConsoleComponent, ConsoleShuttleEvent>(OnCargoGetConsole);
        SubscribeLocalEvent<CargoPilotConsoleComponent, AfterActivatableUIOpenEvent>(OnCargoPilotConsoleOpen);
        SubscribeLocalEvent<CargoPilotConsoleComponent, BoundUIClosedEvent>(OnCargoPilotConsoleClose);

        SubscribeLocalEvent<StationCargoOrderDatabaseComponent, ComponentStartup>(OnCargoOrderStartup);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnCargoFTLTag(EntityUid uid, CargoShuttleComponent component, ref FTLTagEvent args)
    {
        if (args.Handled)
            return;

        // Just saves mappers forgetting.
        args.Handled = true;
        args.Tag = "DockCargo";
    }

    private void ShutdownShuttle()
    {
        _configManager.UnsubValueChanged(CCVars.CargoShuttles, SetCargoShuttleEnabled);
    }

    private void SetCargoShuttleEnabled(bool value)
    {
        if (_enabled == value)
            return;

        _enabled = value;

        if (value)
        {
            Setup();

            foreach (var station in EntityQuery<StationCargoOrderDatabaseComponent>(true))
            {
                AddShuttle(station);
            }
        }
        else
        {
            CleanupShuttle();
        }
    }

    #region Cargo Pilot Console

    private void OnCargoPilotConsoleOpen(EntityUid uid, CargoPilotConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        component.Entity = GetShuttleConsole(uid);
    }

    private void OnCargoPilotConsoleClose(EntityUid uid, CargoPilotConsoleComponent component, BoundUIClosedEvent args)
    {
        component.Entity = null;
    }

    private void OnCargoGetConsole(EntityUid uid, CargoPilotConsoleComponent component, ref ConsoleShuttleEvent args)
    {
        args.Console = GetShuttleConsole(uid);
    }

    private EntityUid? GetShuttleConsole(EntityUid uid)
    {
        var stationUid = _station.GetOwningStation(uid);

        if (!TryComp<StationCargoOrderDatabaseComponent>(stationUid, out var orderDatabase) ||
            !TryComp<CargoShuttleComponent>(orderDatabase.Shuttle, out var shuttle))
        {
            return null;
        }

        return GetShuttleConsole(orderDatabase.Shuttle.Value, shuttle);
    }

    #endregion

    #region Console

    private void UpdateCargoShuttleConsoles(CargoShuttleComponent component)
    {
        // Update pilot consoles that are already open.
        var pilotConsoleQuery = AllEntityQuery<CargoPilotConsoleComponent>();

        while (pilotConsoleQuery.MoveNext(out var uid, out var console))
        {
            var stationUid = _station.GetOwningStation(uid);
            if (stationUid != component.Station)
                continue;

            console.Entity = component.Owner;
        }

        // Update order consoles.
        var shuttleConsoleQuery = AllEntityQuery<CargoShuttleConsoleComponent>();

        while (shuttleConsoleQuery.MoveNext(out var uid, out var console))
        {
            var stationUid = _station.GetOwningStation(uid);
            if (stationUid != component.Station)
                continue;

            UpdateShuttleState(uid, console, stationUid);
        }
    }

    private void OnCargoShuttleConsoleStartup(EntityUid uid, CargoShuttleConsoleComponent component, ComponentStartup args)
    {
        var station = _station.GetOwningStation(uid);
        UpdateShuttleState(uid, component, station);
    }

    private void UpdateShuttleState(EntityUid uid, CargoShuttleConsoleComponent component, EntityUid? station = null)
    {
        TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase);
        TryComp<CargoShuttleComponent>(orderDatabase?.Shuttle, out var shuttle);

        var orders = GetProjectedOrders(orderDatabase, shuttle);
        var shuttleName = orderDatabase?.Shuttle != null ? MetaData(orderDatabase.Shuttle.Value).EntityName : string.Empty;

        _uiSystem.GetUiOrNull(uid, CargoConsoleUiKey.Shuttle)?.SetState(
            new CargoShuttleConsoleBoundUserInterfaceState(
                station != null ? MetaData(station.Value).EntityName : Loc.GetString("cargo-shuttle-console-station-unknown"),
                string.IsNullOrEmpty(shuttleName) ? Loc.GetString("cargo-shuttle-console-shuttle-not-found") : shuttleName,
                orders));
    }

    #endregion

    #region Shuttle

    public EntityUid? GetShuttleConsole(EntityUid uid, CargoShuttleComponent component)
    {
        var query = AllEntityQuery<ShuttleConsoleComponent, TransformComponent>();

        while (query.MoveNext(out var cUid, out var comp, out var xform))
        {
            if (xform.ParentUid != uid)
                continue;

            return cUid;
        }

        return null;
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

        if (space == 0)
            return orders;

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
                var reducedOrder = new CargoOrderData(order.OrderIndex, order.ProductId, cappedAmount, order.Requester, order.Reason);

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
        Setup();

        if (CargoMap == null ||
            component.Shuttle != null ||
            component.CargoShuttleProto == null)
        {
            return;
        }

        var prototype = _protoMan.Index<CargoShuttlePrototype>(component.CargoShuttleProto);
        var possibleNames = _protoMan.Index<DatasetPrototype>(prototype.NameDataset).Values;
        var name = _random.Pick(possibleNames);

        if (!_map.TryLoad(CargoMap.Value, prototype.Path.ToString(), out var gridList))
        {
            _sawmill.Error($"Could not load the cargo shuttle!");
            return;
        }
        var shuttleUid = gridList[0];
        var xform = Transform(shuttleUid);
        MetaData(shuttleUid).EntityName = name;

        // TODO: Something better like a bounds check.
        xform.LocalPosition += 100 * _index;
        var comp = EnsureComp<CargoShuttleComponent>(shuttleUid);
        comp.Station = component.Owner;

        component.Shuttle = shuttleUid;
        UpdateCargoShuttleConsoles(comp);
        _index++;
        _sawmill.Info($"Added cargo shuttle to {ToPrettyString(shuttleUid)}");
    }

    private void SellPallets(CargoShuttleComponent component, StationBankAccountComponent bank)
    {
        double amount = 0;
        var toSell = new HashSet<EntityUid>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var blacklistQuery = GetEntityQuery<CargoSellBlacklistComponent>();

        foreach (var pallet in GetCargoPallets(component))
        {
            // Containers should already get the sell price of their children so can skip those.
            foreach (var ent in _lookup.GetEntitiesIntersecting(pallet.Owner, LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate))
            {
                // Dont sell:
                // - anything already being sold
                // - anything anchored (e.g. light fixtures)
                // - anything blacklisted (e.g. players).
                if (toSell.Contains(ent) ||
                    (xformQuery.TryGetComponent(ent, out var xform) && xform.Anchored))
                    continue;

                if (blacklistQuery.HasComponent(ent))
                    continue;

                var price = _pricing.GetPrice(ent);
                if (price == 0)
                    continue;
                toSell.Add(ent);
                amount += price;
            }
        }

        bank.Balance += (int) amount;
        _sawmill.Debug($"Cargo sold {toSell.Count} entities for {amount}");

        foreach (var ent in toSell)
        {
            Del(ent);
        }
    }

    private void AddCargoContents(CargoShuttleComponent component, StationCargoOrderDatabaseComponent orderDatabase)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var orders = GetProjectedOrders(orderDatabase, component);

        var pads = GetCargoPallets(component);
        DebugTools.Assert(orders.Sum(o => o.Amount) <= pads.Count);

        for (var i = 0; i < pads.Count; i++)
        {
            if (orders.Count == 0)
                break;

            var order = orders[0];
            var coordinates = new EntityCoordinates(component.Owner, xformQuery.GetComponent(_random.PickAndTake(pads).Owner).LocalPosition);
            var item = Spawn(_protoMan.Index<CargoProductPrototype>(order.ProductId).Product, coordinates);
            SpawnAndAttachOrderManifest(item, order, coordinates, component);
            order.Amount--;

            if (order.Amount == 0)
            {
                // Yes this is functioning as a stack, I was too lazy to re-jig the shuttle state event.
                orders.RemoveSwap(0);
                orderDatabase.Orders.Remove(order.OrderIndex);
            }
            else
            {
                orderDatabase.Orders[order.OrderIndex] = order;
            }
        }
    }

    /// <summary>
    /// Printing and attach order manifests to the orders.
    /// </summary>
    private void SpawnAndAttachOrderManifest(EntityUid item, CargoOrderData order, EntityCoordinates coordinates, CargoShuttleComponent component)
    {
        if (!_protoMan.TryIndex(order.ProductId, out CargoProductPrototype? prototype))
            return;

        // spawn a piece of paper.
        var printed = EntityManager.SpawnEntity(component.PrinterOutput, coordinates);

        if (!TryComp<PaperComponent>(printed, out var paper))
            return;

        // fill in the order data
        var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.PrintableOrderNumber));

        MetaData(printed).EntityName = val;

        _paperSystem.SetContent(printed, Loc.GetString(
            "cargo-console-paper-print-text",
            ("orderNumber", order.PrintableOrderNumber),
            ("itemName", prototype.Name),
            ("requester", order.Requester),
            ("reason", order.Reason),
            ("approver", order.Approver ?? string.Empty)),
            paper);

        // attempt to attach the label
        if (TryComp<PaperLabelComponent>(item, out var label))
        {
            _slots.TryInsert(item, label.LabelSlot, printed, null);
        }
    }

    private void OnCargoFTLStarted(EntityUid uid, CargoShuttleComponent component, ref FTLStartedEvent args)
    {
        var xform = Transform(uid);
        var stationUid = component.Station;

        // Called
        if (xform.MapID != CargoMap ||
            !TryComp<StationCargoOrderDatabaseComponent>(stationUid, out var orderDatabase))
        {
            return;
        }

        AddCargoContents(component, orderDatabase);
        UpdateOrders(orderDatabase);
        UpdateCargoShuttleConsoles(component);
    }

    private void OnCargoFTLCompleted(EntityUid uid, CargoShuttleComponent component, ref FTLCompletedEvent args)
    {
        var xform = Transform(uid);
        // Recalled
        if (xform.MapID != CargoMap)
            return;

        var stationUid = component.Station;

        if (TryComp<StationBankAccountComponent>(stationUid, out var bank))
        {
            SellPallets(component, bank);
        }
    }

    #endregion

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        CleanupShuttle();
    }

    private void CleanupShuttle()
    {
        if (CargoMap == null || !_mapManager.MapExists(CargoMap.Value))
        {
            CargoMap = null;
            DebugTools.Assert(!EntityQuery<CargoShuttleComponent>().Any());
            return;
        }

        _mapManager.DeleteMap(CargoMap.Value);
        CargoMap = null;

        // Shuttle may not have been in the cargo dimension (e.g. on the station map) so need to delete.
        var query = AllEntityQuery<CargoShuttleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (TryComp<StationCargoOrderDatabaseComponent>(comp.Station, out var station))
            {
                station.Shuttle = null;
            }
            QueueDel(uid);
        }
    }

    private void Setup()
    {
        if (!_enabled || CargoMap != null && _mapManager.MapExists(CargoMap.Value))
        {
            return;
        }

        // It gets mapinit which is okay... buuutt we still want it paused to avoid power draining.
        CargoMap = _mapManager.CreateMap();
        var mapUid = _mapManager.GetMapEntityId(CargoMap.Value);
        var ftl = EnsureComp<FTLDestinationComponent>(_mapManager.GetMapEntityId(CargoMap.Value));
        ftl.Whitelist = new EntityWhitelist()
        {
            Components = new[]
            {
                _factory.GetComponentName(typeof(CargoShuttleComponent))
            }
        };

        MetaData(mapUid).EntityName = $"Trading post {_random.Next(1000):000}";

        foreach (var comp in EntityQuery<StationCargoOrderDatabaseComponent>(true))
        {
            AddShuttle(comp);
        }

        _console.RefreshShuttleConsoles();
    }
}
