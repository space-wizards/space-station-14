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
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
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
        if (args.Actor is not { Valid: true } player || args.Amount <= 0)
            return;

        var stationUid = _station.GetOwningStation(ent.Owner);
        if (!TryGetOrderDatabase(stationUid, out var orderDatabase)
            || !ProtoMan.Resolve<CargoProductPrototype>(args.CargoProductId, out var product)
            || !GetAvailableProducts(ent).Contains(args.CargoProductId))
            return;

        if (ent.Comp.Mode == CargoOrderConsoleMode.PrintSlip)
        {
            OnAddOrderMessageSlipPrinter(ent, args, product);
            return;
        }

        var data = GetOrderData(args, product, GenerateOrderId(orderDatabase), ent.Comp.Account);
        if (!TryAddOrder(stationUid.Value, data, orderDatabase))
        {
            PlayDenySound(ent);
            return;
        }

        // Log order addition
        _adminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(player):user} added order [orderId:{data.OrderId}, quantity:{data.OrderQuantity}, product:{data.Product}, requester:{data.Requester}, reason:{data.Reason}]"
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
        if (!ProtoMan.Resolve(order.Product, out var product))
        {
            _popup.PopupCursor(Loc.GetString("cargo-console-invalid-product"), args.Actor);
            PlayDenySound(ent);
            return;
        }

        var amount = GetOutstandingOrderCount((station.Value, orderDatabase), order.Account);
        var capacity = orderDatabase.Capacity;

        // Too many orders, avoid them getting spammed in the UI.
        if (amount + order.OrderQuantity > capacity)
        {
            _popup.PopupCursor(Loc.GetString("cargo-console-too-many"), args.Actor);
            PlayDenySound(ent);
            return;
        }

        var cost = product.Cost * order.OrderQuantity;
        var accountBalance = GetBalanceFromAccount((station.Value, bank), order.Account);

        // Not enough balance
        if (cost > accountBalance)
        {
            _popup.PopupCursor(Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)), args.Actor);
            PlayDenySound(ent);
            return;
        }

        var emagged = _emag.CheckFlag(ent.Owner, EmagType.Interaction);
        if (!emagged)
            order.SetApproverData(_identity.GetIdentityShortInfo(player, ent.Owner));

        order.ApprovingConsole = GetNetEntity(ent.Owner);
        order.Approved = true;

        var ev = new FulfillCargoOrderEvent((station.Value, stationData), order);
        RaiseLocalEvent(ref ev);
        ev.FulfillmentEntity ??= station.Value;

        if (!ev.Handled)
        {
            ev.FulfillmentEntity = TryFulfillOrder((station.Value, stationData), order, orderDatabase);

            if (ev.FulfillmentEntity == null)
            {
                _popup.PopupCursor(Loc.GetString("cargo-console-unfulfilled"), args.Actor);
                PlayDenySound(ent);
                order.Approver = null;
                order.ApprovingConsole = null;
                order.Approved = false;
                return;
            }
        }

        _audio.PlayPvs(ApproveSound, ent.Owner);

        if (!emagged)
        {
            var message = Loc.GetString(
                "cargo-console-unlock-approved-order-broadcast",
                ("productName", Loc.GetString(product.Name)),
                ("orderAmount", order.OrderQuantity),
                ("approver", order.Approver ?? string.Empty),
                ("cost", cost)
            );
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

        _popup.PopupCursor(
            Loc.GetString(
                "cargo-console-trade-station",
                ("destination", MetaData(ev.FulfillmentEntity.Value).EntityName)
            ),
            args.Actor
        );

        // Log order approval
        _adminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(player):user} approved order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.Product}, requester:{order.Requester}, reason:{order.Reason}] on account {order.Account} with balance at {accountBalance}"
        );

        orderDatabase.Orders.Remove(order);
        UpdateBankAccount((station.Value, bank), -cost, order.Account);
        UpdateOrders(station.Value);
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
        return RelevantOrders(station, account, approved: true).Sum(order => order.OrderQuantity - order.NumDispatched);
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
        CargoProductPrototype product,
        int quantity,
        string sender,
        string description,
        string destination,
        StationCargoOrderDatabaseComponent component,
        ProtoId<CargoAccountPrototype> account,
        Entity<StationDataComponent> stationData
    )
    {
        // Make an order
        var id = GenerateOrderId(component);
        var order = new CargoOrderData(id, product, quantity, sender, description, account);

        // Approve it now
        order.SetApproverData(destination, sender);
        order.Approved = true;

        // Log order addition
        _adminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"AddAndApproveOrder {description} added order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.Product}, requester:{order.Requester}, reason:{order.Reason}]"
        );

        // Add it to the list
        return TryAddOrder(dbUid, order, component)
            && TryFulfillOrder(stationData, order, component).HasValue;
    }

    private void OnInteractUsingSlip(
        Entity<CargoOrderConsoleComponent> ent,
        ref InteractUsingEvent args,
        CargoSlipComponent slip
    )
    {
        if (slip.OrderQuantity <= 0)
            return;

        var stationUid = _station.GetOwningStation(ent);

        if (!ProtoMan.TryIndex(slip.Product, out var product))
        {
            Log.Error($"Tried to add invalid cargo product {slip.Product} as order!");
            return;
        }

        if (!TryGetOrderDatabase(stationUid, out var orderDatabase)
            || !ent.Comp.AllowedGroups.Contains(product.Group))
            return;

        var orderId = GenerateOrderId(orderDatabase);
        var data = new CargoOrderData(orderId, product, slip.OrderQuantity, slip.Requester, slip.Reason, slip.Account);

        if (!TryAddOrder(stationUid.Value, data, orderDatabase))
        {
            PlayDenySound(ent);
            return;
        }

        // Log order addition
        _audio.PlayPvs(ent.Comp.ScanSound, ent);
        _adminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.User):user} inserted order slip [orderId:{data.OrderId}, quantity:{data.OrderQuantity}, product:{data.Product}, requester:{data.Requester}, reason:{data.Reason}]"
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
        CargoConsoleAddOrderMessage args,
        CargoProductPrototype product
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

        msg.AddMarkupPermissive(
            Loc.GetString(
                "cargo-acquisition-slip-body",
                ("product", product.Name),
                ("description", product.Description),
                ("unit", product.Cost),
                ("amount", args.Amount),
                ("cost", product.Cost * args.Amount),
                ("orderer", args.Requester),
                ("reason", args.Reason)
            )
        );
        _paperSystem.SetContent((label, paper), msg.ToMarkup());

        var slip = EnsureComp<CargoSlipComponent>(label);
        slip.Product = product.ID;
        slip.Requester = args.Requester;
        slip.Reason = args.Reason;
        slip.OrderQuantity = args.Amount;
        slip.Account = ent.Comp.Account;
    }

    private EntityUid? TryFulfillOrder(
        Entity<StationDataComponent> stationData,
        CargoOrderData order,
        StationCargoOrderDatabaseComponent orderDatabase
    )
    {
        // No slots at the trade station
        EntityUid? tradeDestination = null;

        // Try to fulfill from any station where possible, if the pad is not occupied.
        foreach (var trade in GetTradeStations(stationData))
        {
            var tradePads = GetCargoPallets(trade, BuySellType.Buy);

            var freePads = GetFreeCargoPallets(trade, tradePads);
            if (freePads.Count <= order.OrderQuantity) //check if the station has enough free pallets
                continue;

            _random.Shuffle(freePads);
            foreach (var pad in freePads)
            {
                var coordinates = new EntityCoordinates(trade, pad.Transform.LocalPosition);

                if (!FulfillOrder(order, coordinates, orderDatabase.PrinterOutput))
                    continue;

                tradeDestination = trade;
                order.NumDispatched++;
                if (order.OrderQuantity <= order.NumDispatched) //Spawn a crate on free pellets until the order is fulfilled.
                    break;
            }

            if (tradeDestination != null)
                break;
        }

        return tradeDestination;
    }

    /// <summary>
    /// Fulfills the specified cargo order and spawns paper attached to it.
    /// </summary>
    private bool FulfillOrder(
        CargoOrderData order,
        EntityCoordinates spawn,
        string? paperProto
    )
    {
        if (!ProtoMan.Resolve(order.Product, out var product))
            return false;

        // Create the item itself
        var item = Spawn(product.Product, spawn);
        var itemXForm = Transform(item);

        // Ensure the item doesn't start anchored
        _transform.Unanchor(item, itemXForm);

        // Spawn container and insert the item into it if a container is defined.
        if (product.Container is { } productContainer)
        {
            var containerEntity = SpawnAttachedTo(productContainer.Entity, itemXForm.Coordinates);
            _transform.SetLocalRotation(containerEntity, itemXForm.LocalRotation);

            if (
                !_container.TryGetContainer(containerEntity, productContainer.ContainerId, out var container1)
                || !_container.Insert(item, container1, force: true)
            )
            {
                DebugTools.Assert(
                    $"Failed to insert cargo product into its specified container. This indicates an error in the cargo product definition's YAML as the product should be insertable into its container. {nameof(CargoProductPrototype)}: {(ProtoId<CargoProductPrototype>)order.Product.Id}"
                );
                QueueDel(containerEntity);
            }
            else
            {
                item = containerEntity;
            }
        }

        // Create a sheet of paper to write the order details on
        var printed = SpawnAttachedTo(paperProto, spawn);
        if (!_paperQuery.TryComp(printed, out var paper))
            return true;

        // fill in the order data
        var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.OrderId));
        _metaSystem.SetEntityName(printed, val);

        var accountProto = ProtoMan.Index(order.Account);
        _paperSystem.SetContent(
            (printed, paper),
            Loc.GetString(
                "cargo-console-paper-print-text",
                ("orderNumber", order.OrderId),
                ("itemName", product.Name),
                ("orderQuantity", order.OrderQuantity),
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
            )
        );

        // attempt to attach the label to the item
        if (_paperLabelQuery.TryComp(item, out var label))
            _slots.TryInsert(item, label.LabelSlot, printed, null);

        return true;
    }

    private bool TryAddOrder(
        EntityUid dbUid,
        CargoOrderData data,
        StationCargoOrderDatabaseComponent component
    )
    {
        component.Orders.Add(data);
        UpdateOrders(dbUid);
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
        var stationQuery = EntityQueryEnumerator<StationBankAccountComponent>();
        while (stationQuery.MoveNext(out var uid, out var bank))
        {
            if (Timing.CurTime < bank.NextIncomeTime)
                continue;

            bank.NextIncomeTime += bank.IncomeDelay;

            var balanceToAdd = (int)Math.Round(bank.IncreasePerSecond * bank.IncomeDelay.TotalSeconds);
            UpdateBankAccount((uid, bank), balanceToAdd, bank.RevenueDistribution);
        }
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

        _uiSystem.SetUiState(consoleUid,
            CargoConsoleUiKey.Orders,
            new CargoConsoleInterfaceState(
            MetaData(station.Value).EntityName,
            GetOutstandingOrderCount((station.Value, orderDatabase), console.Account),
            orderDatabase.Capacity,
            GetNetEntity(station.Value),
            RelevantOrders((station.Value, orderDatabase), console.Account),
            GetAvailableProducts((consoleUid, console))
        ));
    }

    /// <summary>
    /// Gets orders relevant to this account, i.e. orders on the account directly or orders on behalf of the account in the primary account.
    /// </summary>
    private List<CargoOrderData> RelevantOrders(
        Entity<StationCargoOrderDatabaseComponent> station,
        ProtoId<CargoAccountPrototype> account,
        bool? approved = null
    )
    {
        if (!_bankQuery.TryComp(station, out var bank))
            return [];

        IEnumerable<CargoOrderData> orders;

        if (account != bank.PrimaryAccount)
            orders = station.Comp.Orders.Where(order => order.Account == account);
        else
            orders = station.Comp.Orders;

        return orders.Where(order => approved == null || order.Approved == approved).ToList();
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

    private static CargoOrderData GetOrderData(
        CargoConsoleAddOrderMessage args,
        CargoProductPrototype cargoProduct,
        int id,
        ProtoId<CargoAccountPrototype> account
    )
    {
        return new CargoOrderData(id, cargoProduct, args.Amount, args.Requester, args.Reason, account);
    }

    private void PlayDenySound(Entity<CargoOrderConsoleComponent> ent)
    {
        if (_timing.CurTime < ent.Comp.NextDenySoundTime)
            return;

        ent.Comp.NextDenySoundTime = _timing.CurTime + ent.Comp.DenySoundDelay;
        _audio.PlayPvs(_audio.ResolveSound(ent.Comp.ErrorSound), ent.Owner);
    }
}
