using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Stack;
using Content.Shared.Stacks;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Whitelist;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using Content.Shared.Coordinates;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    /*
     * Handles cargo shuttle mechanics.
     */

    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    public MapId? CargoMap { get; private set; }

    private void InitializeShuttle()
    {
        SubscribeLocalEvent<CargoShuttleComponent, FTLStartedEvent>(OnCargoFTLStarted);
        SubscribeLocalEvent<CargoShuttleComponent, FTLCompletedEvent>(OnCargoFTLCompleted);
        SubscribeLocalEvent<CargoShuttleComponent, FTLTagEvent>(OnCargoFTLTag);

        SubscribeLocalEvent<CargoShuttleConsoleComponent, ComponentStartup>(OnCargoShuttleConsoleStartup);

        SubscribeLocalEvent<CargoPalletConsoleComponent, CargoPalletSellMessage>(OnPalletSale);
        SubscribeLocalEvent<CargoPalletConsoleComponent, CargoPalletAppraiseMessage>(OnPalletAppraise);
        SubscribeLocalEvent<CargoPalletConsoleComponent, BoundUIOpenedEvent>(OnPalletUIOpen);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        _cfgManager.OnValueChanged(CCVars.GridFill, SetGridFill);
    }

    private void ShutdownShuttle()
    {
        _cfgManager.UnsubValueChanged(CCVars.GridFill, SetGridFill);
    }

    private void SetGridFill(bool obj)
    {
        if (obj)
        {
            SetupCargoShuttle();
        }
    }

    private void OnCargoFTLTag(EntityUid uid, CargoShuttleComponent component, ref FTLTagEvent args)
    {
        if (args.Handled)
            return;

        // Just saves mappers forgetting.
        args.Handled = true;
        args.Tag = "DockCargo";
    }

    #region Console

    private void UpdateCargoShuttleConsoles(EntityUid shuttleUid, CargoShuttleComponent component)
    {
        // Update pilot consoles that are already open.
        _console.RefreshDroneConsoles();

        // Update order consoles.
        var shuttleConsoleQuery = AllEntityQuery<CargoShuttleConsoleComponent>();

        while (shuttleConsoleQuery.MoveNext(out var uid, out _))
        {
            var stationUid = _station.GetOwningStation(uid);
            if (stationUid != shuttleUid)
                continue;

            UpdateShuttleState(uid, stationUid);
        }
    }

    private void UpdatePalletConsoleInterface(EntityUid uid)
    {
        var bui = _uiSystem.GetUi(uid, CargoPalletConsoleUiKey.Sale);
        if (Transform(uid).GridUid is not EntityUid gridUid)
        {
            _uiSystem.SetUiState(bui,
            new CargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }
        GetPalletGoods(gridUid, out var toSell, out var amount);
        _uiSystem.SetUiState(bui,
            new CargoPalletConsoleInterfaceState((int) amount, toSell.Count, true));
    }

    private void OnPalletUIOpen(EntityUid uid, CargoPalletConsoleComponent component, BoundUIOpenedEvent args)
    {
        var player = args.Session.AttachedEntity;

        if (player == null)
            return;

        UpdatePalletConsoleInterface(uid);
    }

    /// <summary>
    /// Ok so this is just the same thing as opening the UI, its a refresh button.
    /// I know this would probably feel better if it were like predicted and dynamic as pallet contents change
    /// However.
    /// I dont want it to explode if cargo uses a conveyor to move 8000 pineapple slices or whatever, they are
    /// known for their entity spam i wouldnt put it past them
    /// </summary>

    private void OnPalletAppraise(EntityUid uid, CargoPalletConsoleComponent component, CargoPalletAppraiseMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (player == null)
            return;

        UpdatePalletConsoleInterface(uid);
    }

    private void OnCargoShuttleConsoleStartup(EntityUid uid, CargoShuttleConsoleComponent component, ComponentStartup args)
    {
        var station = _station.GetOwningStation(uid);
        UpdateShuttleState(uid, station);
    }

    private void UpdateShuttleState(EntityUid uid, EntityUid? station = null)
    {
        TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase);
        TryComp<CargoShuttleComponent>(orderDatabase?.Shuttle, out var shuttle);

        var orders = GetProjectedOrders(station ?? EntityUid.Invalid, orderDatabase, shuttle);
        var shuttleName = orderDatabase?.Shuttle != null ? MetaData(orderDatabase.Shuttle.Value).EntityName : string.Empty;

        _uiSystem.GetUiOrNull(uid, CargoConsoleUiKey.Shuttle)?.SetState(
            new CargoShuttleConsoleBoundUserInterfaceState(
                station != null ? MetaData(station.Value).EntityName : Loc.GetString("cargo-shuttle-console-station-unknown"),
                string.IsNullOrEmpty(shuttleName) ? Loc.GetString("cargo-shuttle-console-shuttle-not-found") : shuttleName,
                orders));
    }

    #endregion

    #region Shuttle

    /// <summary>
    /// Returns the orders that can fit on the cargo shuttle.
    /// </summary>
    private List<CargoOrderData> GetProjectedOrders(
        EntityUid shuttleUid,
        StationCargoOrderDatabaseComponent? component = null,
        CargoShuttleComponent? shuttle = null)
    {
        var orders = new List<CargoOrderData>();

        if (component == null || shuttle == null || component.Orders.Count == 0)
            return orders;

        var spaceRemaining = GetCargoSpace(shuttleUid);
        for( var i = 0; i < component.Orders.Count && spaceRemaining > 0; i++)
        {
            var order = component.Orders[i];
            if(order.Approved)
            {
                var numToShip = order.OrderQuantity - order.NumDispatched;
                if (numToShip > spaceRemaining)
                {
                    // We won't be able to fit the whole order on, so make one
                    // which represents the space we do have left:
                    var reducedOrder = new CargoOrderData(order.OrderId,
                            order.ProductId, spaceRemaining, order.Requester, order.Reason);
                    orders.Add(reducedOrder);
                }
                else
                {
                    orders.Add(order);
                }
                spaceRemaining -= numToShip;
            }
        }

        return orders;
    }

    /// <summary>
    /// Get the amount of space the cargo shuttle can fit for orders.
    /// </summary>
    private int GetCargoSpace(EntityUid gridUid)
    {
        var space = GetCargoPallets(gridUid).Count;
        return space;
    }

    private List<(EntityUid Entity, CargoPalletComponent Component)> GetCargoPallets(EntityUid gridUid)
    {
        var pads = new List<(EntityUid, CargoPalletComponent)>();
        var query = AllEntityQuery<CargoPalletComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var compXform))
        {
            if (compXform.ParentUid != gridUid ||
                !compXform.Anchored)
            {
                continue;
            }

            pads.Add((uid, comp));
        }

        return pads;
    }

    #endregion

    #region Station

    private void SellPallets(EntityUid gridUid, out double amount)
    {
        GetPalletGoods(gridUid, out var toSell, out amount);

        _sawmill.Debug($"Cargo sold {toSell.Count} entities for {amount}");

        foreach (var ent in toSell)
        {
            Del(ent);
        }
    }

    private void GetPalletGoods(EntityUid gridUid, out HashSet<EntityUid> toSell, out double amount)
    {
        amount = 0;
        var xformQuery = GetEntityQuery<TransformComponent>();
        var blacklistQuery = GetEntityQuery<CargoSellBlacklistComponent>();
        var mobStateQuery = GetEntityQuery<MobStateComponent>();
        toSell = new HashSet<EntityUid>();

        foreach (var (palletUid, _) in GetCargoPallets(gridUid))
        {
            // Containers should already get the sell price of their children so can skip those.
            foreach (var ent in _lookup.GetEntitiesIntersecting(palletUid, LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate))
            {
                // Dont sell:
                // - anything already being sold
                // - anything anchored (e.g. light fixtures)
                // - anything blacklisted (e.g. players).
                if (toSell.Contains(ent) ||
                    xformQuery.TryGetComponent(ent, out var xform) &&
                    (xform.Anchored || !CanSell(ent, xform, mobStateQuery, xformQuery)))
                {
                    continue;
                }

                if (blacklistQuery.HasComponent(ent))
                    continue;

                var price = _pricing.GetPrice(ent);
                if (price == 0)
                    continue;
                toSell.Add(ent);
                amount += price;
            }
        }
    }

    private bool CanSell(EntityUid uid, TransformComponent xform, EntityQuery<MobStateComponent> mobStateQuery, EntityQuery<TransformComponent> xformQuery)
    {
        if (mobStateQuery.TryGetComponent(uid, out var mobState) &&
            mobState.CurrentState != MobState.Dead)
        {
            return false;
        }

        // Recursively check for mobs at any point.
        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (!CanSell(child.Value, xformQuery.GetComponent(child.Value), mobStateQuery, xformQuery))
                return false;
        }

        return true;
    }

    private void AddCargoContents(EntityUid shuttleUid, CargoShuttleComponent shuttle, StationCargoOrderDatabaseComponent orderDatabase)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        var pads = GetCargoPallets(shuttleUid);
        while (pads.Count > 0)
        {
            var coordinates = new EntityCoordinates(shuttleUid, xformQuery.GetComponent(_random.PickAndTake(pads).Entity).LocalPosition);
            if(!FulfillOrder(orderDatabase, coordinates, shuttle.PrinterOutput))
            {
                break;
            }
        }
    }

    private void OnPalletSale(EntityUid uid, CargoPalletConsoleComponent component, CargoPalletSellMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (player == null)
            return;

        var bui = _uiSystem.GetUi(uid, CargoPalletConsoleUiKey.Sale);
        if (Transform(uid).GridUid is not EntityUid gridUid)
        {
            _uiSystem.SetUiState(bui,
            new CargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }

        SellPallets(gridUid, out var price);
        var stackPrototype = _prototypeManager.Index<StackPrototype>(component.CashType);
        _stack.Spawn((int)price, stackPrototype, uid.ToCoordinates());
        UpdatePalletConsoleInterface(uid);
    }

    private void OnCargoFTLStarted(EntityUid uid, CargoShuttleComponent component, ref FTLStartedEvent args)
    {
        var stationUid = _station.GetOwningStation(uid);

        // Called
        if (CargoMap == null ||
            args.FromMapUid != _mapManager.GetMapEntityId(CargoMap.Value) ||
            !TryComp<StationCargoOrderDatabaseComponent>(stationUid, out var orderDatabase))
        {
            return;
        }

        AddCargoContents(uid, component, orderDatabase);
        UpdateOrders(orderDatabase);
        UpdateCargoShuttleConsoles(uid, component);
    }

    private void OnCargoFTLCompleted(EntityUid uid, CargoShuttleComponent component, ref FTLCompletedEvent args)
    {
        var xform = Transform(uid);
        // Recalled
        if (xform.MapID != CargoMap)
            return;

        var stationUid = _station.GetOwningStation(uid);

        if (TryComp<StationBankAccountComponent>(stationUid, out var bank))
        {
            SellPallets(uid, out var amount);
            bank.Balance += (int) amount;
        }
    }

    #endregion

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        CleanupCargoShuttle();

        if (_cfgManager.GetCVar(CCVars.GridFill))
            SetupCargoShuttle();
    }

    private void CleanupCargoShuttle()
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
            if (TryComp<StationCargoOrderDatabaseComponent>(uid, out var station))
            {
                station.Shuttle = null;
            }

            QueueDel(uid);
        }
    }

    private void SetupCargoShuttle()
    {
        if (CargoMap != null && _mapManager.MapExists(CargoMap.Value))
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

        _console.RefreshShuttleConsoles();
    }
}
