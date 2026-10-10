using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Cargo.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Paper;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<CargoOrderConsoleComponent> ent, ref ComponentInit args)
    {
        var station = _station.GetOwningStation(ent.Owner);
        UpdateOrderState(ent.Owner, station);
    }

    [SubscribeLocalEvent]
    private void OnStationInit(EntityUid uid, StationCargoOrderDatabaseComponent orderDatabase, ComponentInit args)
    {
        orderDatabase.NextOrderCheck = Timing.CurTime + orderDatabase.OrderCheckDelay;
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<CargoOrderConsoleComponent> ent, ref InteractUsingEvent args)
    {
        if (_cashQuery.HasComp(args.Used))
        {
            OnInteractUsingCash(ent, ref args);
        }
        else if (
            _slipQuery.TryComp(args.Used, out var slip)
            && ent.Comp.Mode == CargoOrderConsoleMode.DirectOrder
        )
        {
            OnInteractUsingSlip(ent, ref args, slip);
        }
    }

    [SubscribeLocalEvent]
    private void OnEmagged(Entity<CargoOrderConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction)
            || _emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnRemoveOrderMessage(Entity<CargoOrderConsoleComponent> ent, ref CargoConsoleRemoveOrderMessage args)
    {
        var station = _station.GetOwningStation(ent.Owner);

        // PrintSlip consoles can't remove orders as they can't add orders manually.
        if (ent.Comp.Mode == CargoOrderConsoleMode.PrintSlip
            || !TryGetOrderDatabase(station, out var orderDatabase))
            return;

        RemoveOrder(station.Value, args.OrderId, orderDatabase);
    }

    [SubscribeLocalEvent]
    private void OnOrderUIOpened(Entity<CargoOrderConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        var station = _station.GetOwningStation(ent.Owner);
        UpdateOrderState(ent.Owner, station);
    }

    [SubscribeLocalEvent]
    private void OnAddOrderMessage(Entity<CargoOrderConsoleComponent> ent, ref CargoConsoleAddOrderMessage args)
    {
        if (args.Actor is not { Valid: true } player || args.Basket.Count <= 0)
            return;

        var stationUid = _station.GetOwningStation(ent.Owner);
        if (!TryGetOrderDatabase(stationUid, out var orderDatabase)
            || !IsInAvailableProducts(ent, args.Basket))
            return;

        if (ent.Comp.Mode == CargoOrderConsoleMode.PrintSlip)
        {
            OnAddOrderMessageSlipPrinter(ent, args);
            return;
        }

        var id = GenerateOrderId(orderDatabase);
        var order = new CargoOrderData(id, args.Basket, args.Requester, args.Reason, ent.Comp.Account);

        if (!TryAddOrder(stationUid.Value, order, orderDatabase))
        {
            PlayDenySound(ent);
            return;
        }

        // Log order addition
        var adminString = "";
        foreach (var product in args.Basket)
        {
            adminString += $"{product.Quantity} {ProtoMan.Index<CargoProductPrototype>(product.Product).Name},";
        }

        _adminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(player):user} added order [orderId:{order.OrderId}, products:{adminString} requester:{order.Requester}, reason:{order.Reason}]"
        );
    }

    [SubscribeLocalEvent]
    private void OnApproveOrderMessage(Entity<CargoOrderConsoleComponent> ent, ref CargoConsoleApproveOrderMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (ent.Comp.Mode != CargoOrderConsoleMode.DirectOrder)
            return;

        if (!_accessReaderSystem.IsAllowed(player, ent.Owner))
        {
            _popup.PopupCursor(Loc.GetString("cargo-console-order-not-allowed"), args.Actor);
            PlayDenySound(ent);
            return;
        }

        var station = _station.GetOwningStation(ent.Owner);

        // No station to deduct from.
        if (
            !_bankQuery.TryComp(station, out var bank)
            || !_stationQuery.TryComp(station, out var stationData)
            || !TryGetOrderDatabase(station, out var orderDatabase)
        )
        {
            _popup.PopupCursor(Loc.GetString("cargo-console-station-not-found"), args.Actor);
            PlayDenySound(ent);
            return;
        }

        // Find our order again. It might have been dispatched or approved already
        var orderId = args.OrderId;
        var order = orderDatabase.Orders.Find(order => orderId == order.OrderId && !order.Approved);
        if (order == null || !ProtoMan.Resolve(order.Account, out var account))
            return;

        // Invalid order
        if (!IsInAvailableProducts(ent, order.Basket))
        {
            _popup.PopupCursor(Loc.GetString("cargo-console-invalid-product"), args.Actor);
            PlayDenySound(ent);
            return;
        }

        var amount = GetOutstandingOrderCount((station.Value, orderDatabase), order.Account);
        var capacity = orderDatabase.Capacity;

        // Too many orders, avoid them getting spammed in the UI.
        if (amount >= orderDatabase.Capacity)
        {
            _popup.PopupCursor(Loc.GetString("cargo-console-too-many"), args.Actor);
            PlayDenySound(ent);
            return;
        }

        var cost = GetBasketTotalCost(order.Basket);
        var accountBalance = GetBalanceFromAccount((station.Value, bank), order.Account);

        // Not enough balance
        if (cost > accountBalance)
        {
            _popup.PopupCursor(Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)), args.Actor);
            PlayDenySound(ent);
            return;
        }

        order.ApprovingConsole = GetNetEntity(ent.Owner);
        order.Approved = true;

        _audio.PlayPvs(ApproveSound, ent.Owner);

        if (!_emag.CheckFlag(ent.Owner, EmagType.Interaction))
        {
            order.SetApproverData(_identity.GetIdentityShortInfo(player, ent.Owner));
            var message = GetApprovedRadioMessage(order);
            _radio.SendRadioMessage(ent.Owner, message, account.RadioChannel, ent.Owner, escapeMarkup: false);
            if (CargoOrderConsoleComponent.BaseAnnouncementChannel != account.RadioChannel)
            {
                _radio.SendRadioMessage(
                    ent.Owner,
                    message,
                    CargoOrderConsoleComponent.BaseAnnouncementChannel,
                    ent.Owner,
                    escapeMarkup: false
                );
            }
        }

        // Log order approval
        var adminString = "";
        foreach (var product in order.Basket)
        {
            if (!ProtoMan.TryIndex<CargoProductPrototype>(product.Product, out var productProto))
                continue;
            adminString += $"{product.Quantity} {productProto.Name},";
        }

        _adminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(player):user} approved order [orderId:{order.OrderId}, products:{adminString} requester:{order.Requester}, reason:{order.Reason}] on account {order.Account} with balance at {accountBalance}"
        );

        UpdateBankAccount((station.Value, bank), -cost, order.Account);
        UpdateOrders(station.Value);
        // Prevent unnecessary close checks
        orderDatabase.NextOrderCheck = Timing.CurTime + orderDatabase.OrderCheckDelay;
        TryDeliverAllUndeliveredOrders((station.Value, orderDatabase));
    }

    public void RemoveOrder(
        EntityUid dbUid,
        int index,
        StationCargoOrderDatabaseComponent orderDb
    )
    {
        // Every OrderId is unique
        var sequenceIdx = orderDb.Orders.FindIndex(order => order.OrderId == index);
        if (sequenceIdx != -1)
            orderDb.Orders.RemoveAt(sequenceIdx);

        UpdateOrders(dbUid);
    }

    public void ClearOrders(StationCargoOrderDatabaseComponent component)
    {
        if (component.Orders.Count == 0)
            return;

        component.Orders.Clear();
    }

    public int GetOutstandingOrderCount(
        Entity<StationCargoOrderDatabaseComponent> station,
        ProtoId<CargoAccountPrototype> account
    )
    {
        return RelevantOrders(station, account, approved: true, onlyShowThisAccount: true).Count;
    }

    public List<ProtoId<CargoProductPrototype>> GetAvailableProducts(Entity<CargoOrderConsoleComponent> ent)
    {
        if (
            _station.GetOwningStation(ent) is not { } station
            || !_orderQuery.TryComp(station, out var db)
        )
        {
            return [];
        }

        var products = new List<ProtoId<CargoProductPrototype>>();

        // Note that a market must be both on the station and on the console to be available.
        var markets = ent.Comp.AllowedGroups.Intersect(db.Markets).ToList();
        foreach (var product in ProtoMan.EnumeratePrototypes<CargoProductPrototype>())
        {
            if (!markets.Contains(product.Group))
                continue;

            products.Add(product.ID);
        }

        return products;
    }

    public bool AddAndApproveOrder(
        EntityUid dbUid,
        List<CargoOrderItemData> basket,
        string sender,
        string description,
        string destination,
        StationCargoOrderDatabaseComponent orderDatabase,
        ProtoId<CargoAccountPrototype> account,
        Entity<StationDataComponent> stationData
    )
    {
        // Make an order
        var id = GenerateOrderId(orderDatabase);
        var order = new CargoOrderData(id, basket, sender, description, account);
        order.Visible = false;

        // Approve it now
        order.SetApproverData(destination, sender);
        order.Approved = true;

        // Log order addition
        var adminString = "";
        foreach (var product in order.Basket)
        {
            if (!ProtoMan.TryIndex<CargoProductPrototype>(product.Product, out var productProto))
                continue;
            adminString += $"{product.Quantity} {productProto.Name},";
        }

        _adminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"AddAndApproveOrder {description} added order [orderId:{order.OrderId}, products:{adminString} requester:{order.Requester}, reason:{order.Reason}]"
        );

        // Add it to the list
        return TryAddOrder(dbUid, order, orderDatabase);
    }

    public void TryDeliverAllUndeliveredOrders(Entity<StationCargoOrderDatabaseComponent> ent)
    {
        if (!TryComp<StationDataComponent>(ent, out var stationData))
            return;

        var toDeliver = new List<CargoOrderData>();

        foreach (var order in ent.Comp.Orders)
        {
            // Don't deliver unapproved orders
            if (!order.Approved)
                continue;

            // If the order has been delivered remove from active and add to history
            if (!order.Basket.Any(item => item.NumOrdered < item.Quantity))
            {
                toDeliver.Add(order);
                continue;
            }

            // If the order is already taken by something i.e. telepad
            if (order.Assigned && TryGetEntity(order.AssignedEntity, out var _))
                continue;

            // If something can take the order i.e. telepad
            if (TryExternalFulfillment((ent, stationData), order))
                continue;

            // Try to deliver the order
            // This can partially deliver the order but will return false
            if (TryFulfillOrder((ent, stationData), order, ent.Comp))
                toDeliver.Add(order);
        }

        foreach (var order in toDeliver)
            TryDeliverOrder(order, ent.Comp);

        UpdateOrders(ent);
    }

    private void OnInteractUsingSlip(
        Entity<CargoOrderConsoleComponent> ent,
        ref InteractUsingEvent args,
        CargoSlipComponent slip
    )
    {
        var stationUid = _station.GetOwningStation(ent);

        if (!TryGetOrderDatabase(stationUid, out var orderDatabase))
            return;

        // Invalid order
        if (!IsInAvailableProducts(ent, slip.Basket))
        {
            _popup.PopupCursor(Loc.GetString("cargo-console-invalid-product"), args.User);
            PlayDenySound(ent);
            return;
        }

        var order = new CargoOrderData(GenerateOrderId(orderDatabase), slip.Basket, slip.Requester, slip.Reason, slip.Account);

        if (!TryAddOrder(stationUid.Value, order, orderDatabase))
        {
            PlayDenySound(ent);
            return;
        }

        _audio.PlayPvs(ent.Comp.ScanSound, ent);

        // Log order addition
        var adminString = "";
        foreach (var product in slip.Basket)
        {
            adminString += $"{product.Quantity} {ProtoMan.Index<CargoProductPrototype>(product.Product).Name},";
        }

        _adminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.User):user} inserted order slip [orderId:{order.OrderId}, products:{adminString} requester:{order.Requester}, reason:{order.Reason}]"
        );
        QueueDel(args.Used);
        args.Handled = true;
    }

    private void OnInteractUsingCash(Entity<CargoOrderConsoleComponent> ent, ref InteractUsingEvent args)
    {
        var price = _pricing.GetPrice(args.Used);
        if (price == 0)
            return;

        var stationUid = _station.GetOwningStation(args.Used);

        if (!_bankQuery.TryComp(stationUid, out var bank))
            return;

        _audio.PlayPvs(ApproveSound, ent.Owner);
        UpdateBankAccount((stationUid.Value, bank), (int)price, ent.Comp.Account);
        QueueDel(args.Used);
        args.Handled = true;
    }

    private void OnAddOrderMessageSlipPrinter(
        Entity<CargoOrderConsoleComponent> ent,
        CargoConsoleAddOrderMessage args
    )
    {
        if (!ProtoMan.Resolve(ent.Comp.Account, out var account)
            || Timing.CurTime < ent.Comp.NextPrintTime)
            return;

        var label = Spawn(account.AcquisitionSlip, Transform(ent.Owner).Coordinates);
        ent.Comp.NextPrintTime = Timing.CurTime + ent.Comp.PrintDelay;
        _audio.PlayPvs(ent.Comp.PrintSound, ent.Owner);

        var paper = EnsureComp<PaperComponent>(label);
        var msg = new FormattedMessage();

        msg.AddMarkupPermissive(GetApprovedRadioMessage(new CargoOrderData(0, args.Basket, args.Requester, args.Reason, ent.Comp.Account)));
        _paperSystem.SetContent((label, paper), msg.ToMarkup());

        var slip = EnsureComp<CargoSlipComponent>(label);
        slip.Basket = args.Basket;
        slip.Requester = args.Requester;
        slip.Reason = args.Reason;
        slip.Account = ent.Comp.Account;
    }

    private bool TryFulfillOrder(
        Entity<StationDataComponent> stationData,
        CargoOrderData order,
        StationCargoOrderDatabaseComponent orderDatabase
    )
    {
        var containers = PackOrderIntoContainers(order);
        return TryFulfillOrder(stationData, containers, orderDatabase);
    }

    private bool TryFulfillOrder(
        Entity<StationDataComponent> stationData,
        List<CargoOrderContainerData> containers,
        StationCargoOrderDatabaseComponent orderDatabase
    )
    {
        // Try to fulfill from any station where possible, if the pad is not occupied.
        foreach (var trade in GetTradeStations(stationData))
        {
            var tradePads = GetCargoPallets(trade, BuySellType.Buy);

            var freePads = GetFreeCargoPallets(trade, tradePads);

            _random.Shuffle(freePads);
            foreach (var pad in freePads)
            {
                var coordinates = new EntityCoordinates(trade, pad.Transform.LocalPosition);

                if (!FulfillOrder(containers[0], coordinates, orderDatabase.PrinterOutput))
                    continue;
                containers.RemoveAt(0);
                if (containers.Count == 0)
                    break;
            }
            if (containers.Count == 0)
                break;
        }

        return containers.Count == 0;
    }

    /// <summary>
    /// Fulfills the specified cargo order and spawns paper attached to it.
    /// </summary>
    private bool FulfillOrder(CargoOrderContainerData container, EntityCoordinates spawn, string? paperProto)
    {
        if (!SpawnContainer(container, spawn, out var containerEntity))
            return false;

        var printed = Spawn(paperProto, spawn);
        if (TryComp<PaperComponent>(printed, out var paper))
        {
            _metaSystem.SetEntityName(printed, container.LabelName);

            _paperSystem.SetContent((printed, paper), container.LabelMessage);

            if (TryComp<PaperLabelComponent>(containerEntity, out var label))
                _slots.TryInsert(containerEntity, label.LabelSlot, printed, null);
        }
        return true;
    }

    /// <summary>
    /// Spawns a CargoOrderContainerData container with all its contents.
    /// </summary>
    public bool SpawnContainer(
        CargoOrderContainerData container,
        EntityCoordinates spawn,
        out EntityUid containerEntity
    )
    {
        containerEntity = EntityUid.Invalid;

        // If product is one entity i.e. crate fill entity or single object
        if (container.IsSingleProduct || container.Container == "")
        {
            var first = container.Products.First();
            if (!ProtoMan.Resolve(first.Source.Product, out var singleProto))
                return false;
            containerEntity = Spawn(singleProto.SpawnList.First(), spawn);
            first.Source.NumOrdered++;
            _transform.Unanchor(containerEntity, Transform(containerEntity));
            return true;
        }

        containerEntity = Spawn(container.Container, spawn);
        if (!containerEntity.IsValid())
            return false;

        _transform.Unanchor(containerEntity, Transform(containerEntity));

        foreach (var item in container.Products)
        {
            if (!ProtoMan.Resolve(item.Source.Product, out var productProto))
                continue;

            if (container.Container == null || !_container.TryGetContainer(containerEntity, container.ContainerID!, out var slot))
            {
                DebugTools.Assert(
                    false,
                    $"Failed to find container slot for cargo product. Check the container definition. {productProto.Name}: {container.Container}"
                );
                continue;
            }

            for (int i = 0; i < item.Quantity; i++)
            {
                foreach (var product in productProto.SpawnList)
                {
                    var itemEntity = Spawn(product, spawn);
                    if (!_container.Insert(itemEntity, slot, force: true))
                    {
                        DebugTools.Assert(
                            false,
                            $"Failed to insert cargo product into its specified container. This indicates an error in the cargo product definition's YAML as the product should be insertable into its container. {productProto.Name}: {container.Container}"
                        );
                    }
                }
                item.Source.NumOrdered++;
            }
        }

        return true;
    }

    /// <summary>
    /// Sorts the items in an order into containers.
    /// It is sorted to use as few containers as needed.
    /// Any containers with only 1 item left that is allowed will become a parcel
    /// </summary>
    private List<CargoOrderContainerData> PackOrderIntoContainers(CargoOrderData order)
    {
        var containers = PackBasketIntoContainers(ref order.Basket);

        ApplyLabels(containers, order);
        return containers;
    }

    private void ApplyLabels(List<CargoOrderContainerData> containers, CargoOrderData order)
    {
        foreach (var container in containers)
        {
            container.LabelMessage = GetContainerLabel(container, order);
            container.LabelName = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.OrderId));
        }
    }

    /// <summary>
    /// Create the string which will go on the label of the container
    /// </summary>
    private string GetContainerLabel(CargoOrderContainerData container, CargoOrderData order)
    {
        var accountProto = ProtoMan.Index(order.Account);
        string message;
        if (container.IsSingleProduct)
        {
            if (!ProtoMan.TryIndex<CargoProductPrototype>(container.Products[0].Source.Product, out var singleProto))
                return "";
            message = Loc.GetString(
                "cargo-console-paper-print-text",
                ("orderNumber", order.OrderId),
                ("itemName", Loc.GetString(singleProto!.Name)),
                ("requester", order.Requester),
                (
                    "reason",
                    string.IsNullOrWhiteSpace(order.Reason)
                        ? Loc.GetString("cargo-console-paper-reason-default")
                        : order.Reason
                ),
                ("account", Loc.GetString(accountProto.Name)),
                ("accountcode", Loc.GetString(accountProto.Code)),
                (
                    "approver",
                    string.IsNullOrWhiteSpace(order.Approver)
                        ? Loc.GetString("cargo-console-paper-approver-default")
                        : order.Approver
                )
            );
            return message;
        }
        message = Loc.GetString("cargo-console-paper-print-header", ("orderNumber", order.OrderId));
        message += "\n";
        foreach (var product in container.Products)
        {
            if (!ProtoMan.TryIndex<CargoProductPrototype>(product.Source.Product, out var productProto))
            {
                message += "\n";
                continue;
            }
            message += Loc.GetString(
                "cargo-console-paper-print-item",
                ("itemName", Loc.GetString(productProto.Name)),
                ("orderQuantity", product.Quantity)
            );
            message += "\n";
        }
        message += Loc.GetString(
            "cargo-console-paper-print-footer",
            ("requester", order.Requester),
            (
                "reason",
                string.IsNullOrWhiteSpace(order.Reason)
                    ? Loc.GetString("cargo-console-paper-reason-default")
                    : order.Reason
            ),
            ("account", Loc.GetString(accountProto.Name)),
            ("accountcode", Loc.GetString(accountProto.Code)),
            (
                "approver",
                string.IsNullOrWhiteSpace(order.Approver)
                    ? Loc.GetString("cargo-console-paper-approver-default")
                    : order.Approver
            )
        );
        return message;
    }

    /// <summary>
    /// Check if all products in a basket are avalible on a ordering console
    /// </summary>
    public bool IsInAvailableProducts(Entity<CargoOrderConsoleComponent> ent, List<CargoOrderItemData> basket)
    {
        var availableProducts = GetAvailableProducts(ent);
        foreach (var product in basket)
        {
            if (!ProtoMan.TryIndex<CargoProductPrototype>(product.Product, out var _))
            {
                Log.Error($"Tried to add invalid cargo product {product.Product} as order!");
                return false;
            }
            if (!availableProducts.Contains(product.Product))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryAddOrder(
        EntityUid dbUid,
        CargoOrderData order,
        StationCargoOrderDatabaseComponent orderDatabase
    )
    {
        var outstanding = GetOutstandingOrderCount((dbUid, orderDatabase), order.Account);
        if (outstanding >= orderDatabase.Capacity)
            return false;

        orderDatabase.Orders.Add(order);
        UpdateOrders(dbUid);
        return true;
    }

    private bool TryDeliverOrder(
        CargoOrderData order,
        StationCargoOrderDatabaseComponent orderDatabase
    )
    {
        orderDatabase.Orders.Remove(order);
        orderDatabase.DeliveredOrders.Add(order);
        // Prevent unbounded growth of delivered orders.
        if (orderDatabase.DeliveredOrders.Count > 1000)
            orderDatabase.DeliveredOrders.RemoveAt(0);
        return true;
    }

    private static int GenerateOrderId(StationCargoOrderDatabaseComponent orderDb)
    {
        // We need an arbitrary unique ID to identify orders, since they may
        // want to be cancelled later.
        return ++orderDb.NumOrdersCreated;
    }

    private void UpdateConsole()
    {
        var stationQuery = EntityQueryEnumerator<StationBankAccountComponent, StationCargoOrderDatabaseComponent>();
        while (stationQuery.MoveNext(out var uid, out var bank, out var orderDatabase))
        {
            if (Timing.CurTime > bank.NextIncomeTime)
            {
                bank.NextIncomeTime += bank.IncomeDelay;

                var balanceToAdd = (int)Math.Round(bank.IncreasePerSecond * bank.IncomeDelay.TotalSeconds);
                UpdateBankAccount((uid, bank), balanceToAdd, bank.RevenueDistribution);
            }
            if (Timing.CurTime > orderDatabase.NextOrderCheck)
            {
                orderDatabase.NextOrderCheck += orderDatabase.OrderCheckDelay;

                TryDeliverAllUndeliveredOrders((uid, orderDatabase));
            }
        }
    }

    private bool TryExternalFulfillment(Entity<StationDataComponent> station, CargoOrderData order)
    {
        var ev = new FulfillCargoOrderEvent(station, order);
        RaiseLocalEvent(ref ev);

        if (!ev.Handled || !TryGetNetEntity(ev.FulfillmentEntity, out var netEnt))
            return false;

        order.Assigned = true;
        order.AssignedEntity = netEnt;
        return true;
    }

    /// <summary>
    /// Updates all of the cargo-related consoles for a particular station.
    /// This should be called whenever orders change.
    /// </summary>
    private void UpdateOrders(EntityUid dbUid)
    {
        // Order added so all consoles need updating.
        var orderQuery = AllEntityQuery<CargoOrderConsoleComponent>();

        while (orderQuery.MoveNext(out var uid, out var _))
        {
            var station = _station.GetOwningStation(uid);
            if (station != dbUid)
                continue;

            UpdateOrderState(uid, station);
        }
    }

    private void UpdateOrderState(EntityUid consoleUid, EntityUid? station)
    {
        if (!_consoleQuery.TryComp(consoleUid, out var console)
            || !_orderQuery.TryComp(station, out var orderDatabase)
            || !_uiSystem.HasUi(consoleUid, CargoConsoleUiKey.Orders))
            return;

        var orderHistory = orderDatabase
            .DeliveredOrders.Concat(orderDatabase.Orders)
            .OrderBy(order => order.OrderId)
            .ToList();
        _uiSystem.SetUiState(
            consoleUid,
            CargoConsoleUiKey.Orders,
            new CargoConsoleInterfaceState(
                MetaData(station.Value).EntityName,
                GetOutstandingOrderCount((station!.Value, orderDatabase), console.Account),
                orderDatabase.Capacity,
                GetNetEntity(station.Value),
                RelevantOrders((station.Value, orderDatabase), orderDatabase.Orders, console.Account, approved: false),
                RelevantOrders((station.Value, orderDatabase), orderHistory, console.Account, approved: true),
                GetAvailableProducts((consoleUid, console))
            )
        );
    }

    private List<CargoOrderData> RelevantOrders(
        Entity<StationCargoOrderDatabaseComponent> station,
        ProtoId<CargoAccountPrototype> account,
        bool? approved = null,
        bool onlyShowThisAccount = false
    )
    {
        return RelevantOrders(station, station.Comp.Orders, account, approved, onlyShowThisAccount);
    }


    /// <summary>
    /// Gets orders relevant to this account, i.e. orders on the account directly or orders on behalf of the account in the primary account.
    /// </summary>
    private List<CargoOrderData> RelevantOrders(
        Entity<StationCargoOrderDatabaseComponent> station,
        List<CargoOrderData> allOrders,
        ProtoId<CargoAccountPrototype> account,
        bool? approved = null,
        bool onlyShowThisAccount = false
    )
    {
        if (!_bankQuery.TryComp(station, out var bank))
            return [];

        IEnumerable<CargoOrderData> orders;

        if (onlyShowThisAccount || account != bank.PrimaryAccount)
            orders = allOrders.Where(order => order.Account == account);
        else
            orders = allOrders;

        return [.. orders.Where(order => order.Visible && (approved == null || order.Approved == approved))];
    }

    private bool TryGetOrderDatabase(
        [NotNullWhen(true)] EntityUid? stationUid,
        [MaybeNullWhen(false)] out StationCargoOrderDatabaseComponent dbComp
    )
    {
        return _orderQuery.TryComp(stationUid, out dbComp);
    }

    private IEnumerable<EntityUid> GetTradeStations(StationDataComponent data)
    {
        foreach (var gridUid in data.Grids)
        {
            if (!_tradeStationQuery.HasComponent(gridUid))
                continue;

            yield return gridUid;
        }
    }

    private void PlayDenySound(Entity<CargoOrderConsoleComponent> ent)
    {
        if (_timing.CurTime < ent.Comp.NextDenySoundTime)
            return;

        ent.Comp.NextDenySoundTime = _timing.CurTime + ent.Comp.DenySoundDelay;
        _audio.PlayPvs(_audio.ResolveSound(ent.Comp.ErrorSound), ent.Owner);
    }

    private string GetApprovedRadioMessage(CargoOrderData order)
    {
        var message = Loc.GetString(
            "cargo-console-unlock-approved-order-broadcast-header",
            ("orderID", order.OrderId)
        );
        message += "\n";
        foreach (var product in order.Basket)
        {
            message += Loc.GetString(
                "cargo-console-unlock-approved-order-broadcast-item",
                ("productName", Loc.GetString(ProtoMan.Index<CargoProductPrototype>(product.Product).Name)),
                ("orderAmount", product.Quantity)
            );
            message += "\n";
        }
        message += Loc.GetString(
            "cargo-console-unlock-approved-order-broadcast-footer",
            ("approver", order.Approver ?? Loc.GetString("cargo-console-paper-approver-default")),
            ("cost", GetBasketTotalCost(order.Basket))
        );
        return message;
    }
}
