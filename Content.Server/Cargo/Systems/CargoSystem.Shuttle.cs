using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Labels.Components;
using Content.Server.MobState;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.UserInterface;
using Content.Server.Paper;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.GameTicking;
using Content.Shared.MobState.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    /*
     * Handles cargo shuttle mechanics, including cargo shuttle consoles.
     */

    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;

    public MapId? CargoMap { get; private set; }

    private const float CallOffset = 50f;

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

        SubscribeLocalEvent<CargoShuttleComponent, MoveEvent>(OnCargoShuttleMove);
        SubscribeLocalEvent<CargoShuttleConsoleComponent, ComponentStartup>(OnCargoShuttleConsoleStartup);
        SubscribeLocalEvent<CargoShuttleConsoleComponent, CargoCallShuttleMessage>(OnCargoShuttleCall);
        SubscribeLocalEvent<CargoShuttleConsoleComponent, CargoRecallShuttleMessage>(RecallCargoShuttle);

        SubscribeLocalEvent<CargoPilotConsoleComponent, ConsoleShuttleEvent>(OnCargoGetConsole);
        SubscribeLocalEvent<CargoPilotConsoleComponent, AfterActivatableUIOpenEvent>(OnCargoPilotConsoleOpen);
        SubscribeLocalEvent<CargoPilotConsoleComponent, BoundUIClosedEvent>(OnCargoPilotConsoleClose);

        SubscribeLocalEvent<StationCargoOrderDatabaseComponent, ComponentStartup>(OnCargoOrderStartup);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void ShutdownShuttle()
    {
        _configManager.UnsubValueChanged(CCVars.CargoShuttles, SetCargoShuttleEnabled);
    }

    private void SetCargoShuttleEnabled(bool value)
    {
        if (_enabled == value) return;
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
        component.Entity = GetShuttleConsole(component);
    }

    private void OnCargoPilotConsoleClose(EntityUid uid, CargoPilotConsoleComponent component, BoundUIClosedEvent args)
    {
        component.Entity = null;
    }

    private void OnCargoGetConsole(EntityUid uid, CargoPilotConsoleComponent component, ref ConsoleShuttleEvent args)
    {
        args.Console = GetShuttleConsole(component);
    }

    private EntityUid? GetShuttleConsole(CargoPilotConsoleComponent component)
    {
        var stationUid = _station.GetOwningStation(component.Owner);

        if (!TryComp<StationCargoOrderDatabaseComponent>(stationUid, out var orderDatabase) ||
            !TryComp<CargoShuttleComponent>(orderDatabase.Shuttle, out var shuttle)) return null;

        return GetShuttleConsole(shuttle);
    }

    #endregion

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
        var shuttleName = orderDatabase?.Shuttle != null ? MetaData(orderDatabase.Shuttle.Value).EntityName : string.Empty;

        _uiSystem.GetUiOrNull(component.Owner, CargoConsoleUiKey.Shuttle)?.SetState(
            new CargoShuttleConsoleBoundUserInterfaceState(
                station != null ? MetaData(station.Value).EntityName : Loc.GetString("cargo-shuttle-console-station-unknown"),
                string.IsNullOrEmpty(shuttleName) ? Loc.GetString("cargo-shuttle-console-shuttle-not-found") : shuttleName,
                _shuttle.CanFTL(shuttle?.Owner, out _),
                shuttle?.NextCall,
                orders));
    }

    #endregion

    #region Shuttle

    public EntityUid? GetShuttleConsole(CargoShuttleComponent component)
    {
        foreach (var (comp, xform) in EntityQuery<ShuttleConsoleComponent, TransformComponent>(true))
        {
            if (xform.ParentUid != component.Owner) continue;
            return comp.Owner;
        }

        return null;
    }

    private void OnCargoShuttleMove(EntityUid uid, CargoShuttleComponent component, ref MoveEvent args)
    {
        if (component.Station == null) return;

        var oldCanRecall = component.CanRecall;

        // Check if we can update the recall status.
        var canRecall = _shuttle.CanFTL(uid, out _, args.Component);
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
                var reducedOrder = new CargoOrderData(order.OrderNumber, order.ProductId, cappedAmount, order.Requester, order.Reason);

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
            component.CargoShuttleProto == null) return;

        var prototype = _protoMan.Index<CargoShuttlePrototype>(component.CargoShuttleProto);
        var possibleNames = _protoMan.Index<DatasetPrototype>(prototype.NameDataset).Values;
        var name = _random.Pick(possibleNames);

        var shuttleUid = _map.LoadGrid(CargoMap.Value, prototype.Path.ToString());
        var xform = Transform(shuttleUid!.Value);
        MetaData(shuttleUid!.Value).EntityName = name;

        // TODO: Something better like a bounds check.
        xform.LocalPosition += 100 * _index;
        var comp = EnsureComp<CargoShuttleComponent>(shuttleUid!.Value);
        comp.Station = component.Owner;
        comp.Coordinates = xform.Coordinates;

        component.Shuttle = shuttleUid;
        comp.NextCall = _timing.CurTime + TimeSpan.FromSeconds(comp.Cooldown);
        UpdateShuttleCargoConsoles(comp);
        _index++;
        _sawmill.Info($"Added cargo shuttle to {ToPrettyString(shuttleUid!.Value)}");
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

    private void SendToCargoMap(EntityUid uid, CargoShuttleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.NextCall = _timing.CurTime + TimeSpan.FromSeconds(component.Cooldown);
        Transform(uid).Coordinates = component.Coordinates;
        DebugTools.Assert(MetaData(uid).EntityPaused);

        UpdateShuttleCargoConsoles(component);
        _sawmill.Info($"Stashed cargo shuttle {ToPrettyString(uid)}");
    }

    /// <summary>
    /// Retrieves a shuttle for delivery.
    /// </summary>
    public void CallShuttle(StationCargoOrderDatabaseComponent orderDatabase)
    {
        if (!TryComp<CargoShuttleComponent>(orderDatabase.Shuttle, out var shuttle))
            return;

        // Already called / not available
        if (shuttle.NextCall == null || _timing.CurTime < shuttle.NextCall)
            return;

        if (!TryComp<StationDataComponent>(orderDatabase.Owner, out var stationData))
            return;

        var targetGrid = _station.GetLargestGrid(stationData);

        // Nowhere to warp in to.
        if (!TryComp<TransformComponent>(targetGrid, out var xform))
            return;

        shuttle.NextCall = null;

        // Find a valid free area nearby to spawn in on
        // TODO: Make this use hyperspace now.
        var center = new Vector2();
        var minRadius = 0f;
        Box2? aabb = null;
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var grid in _mapManager.GetAllMapGrids(xform.MapID))
        {
            var worldAABB = xformQuery.GetComponent(grid.Owner).WorldMatrix.TransformBox(grid.LocalAABB);
            aabb = aabb?.Union(worldAABB) ?? worldAABB;
        }

        if (aabb != null)
        {
            center = aabb.Value.Center;
            minRadius = MathF.Max(aabb.Value.Width, aabb.Value.Height);
        }

        var offset = 0f;
        if (TryComp<MapGridComponent>(orderDatabase.Shuttle, out var shuttleGrid))
        {
            var bounds = shuttleGrid.LocalAABB;
            offset = MathF.Max(bounds.Width, bounds.Height) / 2f;
        }

        Transform(shuttle.Owner).Coordinates = new EntityCoordinates(xform.ParentUid,
            center + _random.NextVector2(minRadius + offset, minRadius + CallOffset + offset));
        DebugTools.Assert(!MetaData(shuttle.Owner).EntityPaused);

        AddCargoContents(shuttle, orderDatabase);
        UpdateOrders(orderDatabase);
        UpdateShuttleCargoConsoles(shuttle);
        _console.RefreshShuttleConsoles();

        _sawmill.Info($"Retrieved cargo shuttle {ToPrettyString(shuttle.Owner)} from {ToPrettyString(orderDatabase.Owner)}");
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
                orderDatabase.Orders.Remove(order.OrderNumber);
            }
            else
            {
                orderDatabase.Orders[order.OrderNumber] = order;
            }
        }
    }

    /// <summary>
    /// In this method we are printing and attaching order manifests to the orders.
    /// </summary>
    private void SpawnAndAttachOrderManifest(EntityUid item, CargoOrderData order, EntityCoordinates coordinates, CargoShuttleComponent component)
    {
        if (!_protoMan.TryIndex(order.ProductId, out CargoProductPrototype? prototype))
            return;

        // spawn a piece of paper.
        var printed = EntityManager.SpawnEntity("Paper", coordinates);

        if (!TryComp<PaperComponent>(printed, out var paper))
            return;

        // fill in the order data
        var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.OrderNumber));

        MetaData(printed).EntityName = val;

        _paperSystem.SetContent(printed, Loc.GetString(
            "cargo-console-paper-print-text",
            ("orderNumber", order.OrderNumber),
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

    private void RecallCargoShuttle(EntityUid uid, CargoShuttleConsoleComponent component, CargoRecallShuttleMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (player == null) return;

        var stationUid = _station.GetOwningStation(component.Owner);

        if (!TryComp<StationCargoOrderDatabaseComponent>(stationUid, out var orderDatabase) ||
            !TryComp<StationBankAccountComponent>(stationUid, out var bank)) return;

        if (!TryComp<CargoShuttleComponent>(orderDatabase.Shuttle, out var shuttle))
        {
            _popup.PopupEntity(Loc.GetString("cargo-no-shuttle"), args.Entity, args.Entity);
            return;
        }

        if (!_shuttle.CanFTL(shuttle.Owner, out var reason))
        {
            _popup.PopupEntity(reason, args.Entity, args.Entity);
            return;
        }

        if (IsBlocked(shuttle))
        {
            _popup.PopupEntity(Loc.GetString("cargo-shuttle-console-organics"), player.Value, player.Value);
            SoundSystem.Play(component.DenySound.GetSound(), Filter.Pvs(uid, entityManager: EntityManager), uid);
            return;
        };

        SellPallets(shuttle, bank);
        _console.RefreshShuttleConsoles();
        SendToCargoMap(orderDatabase.Shuttle.Value);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="component"></param>
    private bool IsBlocked(CargoShuttleComponent component)
    {
        // TODO: Would be good to rate-limit this on the console.
        var mobQuery = GetEntityQuery<MobStateComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        return FoundOrganics(component.Owner, mobQuery, xformQuery);
    }

    public bool FoundOrganics(EntityUid uid, EntityQuery<MobStateComponent> mobQuery, EntityQuery<TransformComponent> xformQuery)
    {
        var xform = xformQuery.GetComponent(uid);
        var childEnumerator = xform.ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            if ((mobQuery.TryGetComponent(child.Value, out var mobState) && !_mobState.IsDead(child.Value, mobState))
                || FoundOrganics(child.Value, mobQuery, xformQuery)) return true;
        }

        return false;
    }

    private void OnCargoShuttleCall(EntityUid uid, CargoShuttleConsoleComponent component, CargoCallShuttleMessage args)
    {
        var stationUid = _station.GetOwningStation(args.Entity);
        if (!TryComp<StationCargoOrderDatabaseComponent>(stationUid, out var orderDatabase)) return;
        CallShuttle(orderDatabase);
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
        foreach (var comp in EntityQuery<CargoShuttleComponent>())
        {
            if (TryComp<StationCargoOrderDatabaseComponent>(comp.Station, out var station))
            {
                station.Shuttle = null;
            }
            QueueDel(comp.Owner);
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
        _mapManager.SetMapPaused(CargoMap!.Value, true);

        foreach (var comp in EntityQuery<StationCargoOrderDatabaseComponent>(true))
        {
            AddShuttle(comp);
        }
    }
}
