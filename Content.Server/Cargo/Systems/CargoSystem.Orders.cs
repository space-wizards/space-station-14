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

namespace Content.Server.Cargo.Systems
{
    public sealed partial class CargoSystem
    {
        [Dependency] private SharedTransformSystem _transformSystem = default!;
        [Dependency] private EmagSystem _emag = default!;
        [Dependency] private IGameTiming _timing = default!;

        private void InitializeConsole()
        {
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleAddOrderMessage>(OnAddOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleRemoveOrderMessage>(OnRemoveOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleApproveOrderMessage>(OnApproveOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, BoundUIOpenedEvent>(OnOrderUIOpened);
            SubscribeLocalEvent<CargoOrderConsoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<CargoOrderConsoleComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<CargoOrderConsoleComponent, GotEmaggedEvent>(OnEmagged);
            SubscribeLocalEvent<StationCargoOrderDatabaseComponent, MapInitEvent>(OnMapInit);
        }

        private void OnInteractUsingCash(EntityUid uid, CargoOrderConsoleComponent component, ref InteractUsingEvent args)
        {
            var price = _pricing.GetPrice(args.Used);

            if (price == 0)
                return;

            var stationUid = _station.GetOwningStation(args.Used);

            if (!TryComp(stationUid, out StationBankAccountComponent? bank))
                return;

            _audio.PlayPvs(ApproveSound, uid);
            UpdateBankAccount((stationUid.Value, bank), (int)price, component.Account);
            QueueDel(args.Used);
            args.Handled = true;
        }

        private void OnInteractUsing(EntityUid uid, CargoOrderConsoleComponent component, ref InteractUsingEvent args)
        {
            if (HasComp<CashComponent>(args.Used))
            {
                OnInteractUsingCash(uid, component, ref args);
            }
        }

        private void OnInit(EntityUid uid, CargoOrderConsoleComponent orderConsole, ComponentInit args)
        {
            var station = _station.GetOwningStation(uid);
            UpdateOrderState(uid, station);
        }

        private void OnMapInit(Entity<StationCargoOrderDatabaseComponent> ent, ref MapInitEvent args)
        {
            ent.Comp.NextOrderCheck = Timing.CurTime + ent.Comp.OrderCheckDelay;
        }

        private void OnEmagged(Entity<CargoOrderConsoleComponent> ent, ref GotEmaggedEvent args)
        {
            if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
                return;

            if (_emag.CheckFlag(ent, EmagType.Interaction))
                return;

            args.Handled = true;
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

                    UpdateUndeliveredOrders((uid, orderDatabase));
                }
            }
        }

        private void OnApproveOrderMessage(
            EntityUid uid,
            CargoOrderConsoleComponent component,
            CargoConsoleApproveOrderMessage args
        )
        {
            if (args.Actor is not { Valid: true } player)
                return;

            if (component.Mode != CargoOrderConsoleMode.DirectOrder)
                return;

            if (!_accessReaderSystem.IsAllowed(player, uid))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
                PlayDenySound(uid, component);
                return;
            }

            var station = _station.GetOwningStation(uid);

            // No station to deduct from.
            if (
                !TryComp(station, out StationBankAccountComponent? bank)
                || !TryComp(station, out StationDataComponent? stationData)
                || !TryGetOrderDatabase(station, out var orderDatabase)
            )
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-station-not-found"));
                PlayDenySound(uid, component);
                return;
            }

            // Find our order again. It might have been dispatched or approved already
            var order = orderDatabase.Orders.Find(order => args.OrderId == order.OrderId && !order.Approved);
            if (order == null || !_protoMan.Resolve(order.Account, out var account))
            {
                return;
            }

            // Invalid order
            if (!IsInAvailableProducts((uid, component), order.Basket))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-invalid-product"));
                PlayDenySound(uid, component);
                return;
            }

            // Too many orders, avoid them getting spammed in the UI.
            if (GetOutstandingOrderCount((station.Value, orderDatabase), order.Account) >= orderDatabase.Capacity)
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-too-many"));
                PlayDenySound(uid, component);
                return;
            }

            var cost = GetBasketTotalCost(order.Basket);
            var accountBalance = GetBalanceFromAccount((station.Value, bank), order.Account);

            // Not enough balance
            if (cost > accountBalance)
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)));
                PlayDenySound(uid, component);
                return;
            }

            var emagged = _emag.CheckFlag(uid, EmagType.Interaction);

            if (!emagged)
            {
                order.SetApproverData(_identity.GetIdentityShortInfo(player, uid));
            }

            var ev = new FulfillCargoOrderEvent((station.Value, stationData), order, (uid, component));
            RaiseLocalEvent(ref ev);
            ev.FulfillmentEntity ??= station.Value;

            if (!ev.Handled)
            {
                ev.FulfillmentEntity = TryFulfillOrder((station.Value, stationData), order.Account, order, orderDatabase);

                if (ev.FulfillmentEntity == null)
                {
                    ConsolePopup(args.Actor, Loc.GetString("cargo-console-unfulfilled"));
                    PlayDenySound(uid, component);
                    order.Approver = null;
                    return;
                }
            }

            order.Approved = true;
            order.ApprovingConsole = GetNetEntity(uid);

            _audio.PlayPvs(ApproveSound, uid);

            if (!emagged)
            {
                var message = Loc.GetString("cargo-console-unlock-approved-order-broadcast",
                    ("productName", Loc.GetString(product.Name)),
                    ("orderAmount", order.OrderQuantity),
                    ("approver", order.Approver ?? string.Empty),
                    ("cost", cost));
                _radio.SendRadioMessage(uid, message, account.RadioChannel, uid, escapeMarkup: false);
                if (CargoOrderConsoleComponent.BaseAnnouncementChannel != account.RadioChannel)
                    _radio.SendRadioMessage(
                        uid,
                        message,
                        CargoOrderConsoleComponent.BaseAnnouncementChannel,
                        uid,
                        escapeMarkup: false
                    );
            }

            // Log order approval
            var adminString = "";
            foreach (var product in order.Basket)
            {
                if (!_protoMan.TryIndex<CargoProductPrototype>(product.Product, out var productProto))
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
            UpdateUndeliveredOrders((station.Value, orderDatabase));
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
                    ("productName", Loc.GetString(_protoMan.Index<CargoProductPrototype>(product.Product).Name)),
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
            // No slots at the trade station
            var tradeStations = EntityQueryEnumerator<TradeStationComponent>();

            // Try to fulfill from any station where possible, if the pad is not occupied.
            while (tradeStations.MoveNext(out var trade, out var _))
            {
                foreach (var pad in GetFreeCargoPallets(trade).Shuffle())
                {
                    var coordinates = new EntityCoordinates(trade, pad.Transform.LocalPosition);

                    if (FulfillOrder(containers[0], coordinates, orderDatabase.PrinterOutput))
                    {
                        containers.RemoveAt(0);
                        if (containers.Count == 0)
                            break;
                    }
                }
                if (containers.Count == 0)
                    break;
            }

            return tradeDestination;
        }

        private void GetTradeStations(StationDataComponent data, ref List<EntityUid> ents)
        {
            foreach (var gridUid in data.Grids)
            {
                if (!_tradeStationQuery.HasComponent(gridUid))
                    continue;

                ents.Add(gridUid);
            }
        }

        private void OnRemoveOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleRemoveOrderMessage args)
        {
            var station = _station.GetOwningStation(uid);

            if (component.Mode == CargoOrderConsoleMode.PrintSlip)
                return;

            if (!TryGetOrderDatabase(station, out var orderDatabase))
                return;

            if (!TryComp<StationBankAccountComponent>(station, out var bank))
                return;

            var targetAccount = component.Mode == CargoOrderConsoleMode.SendToPrimary ? bank.PrimaryAccount : component.Account;

            RemoveOrder(station.Value, targetAccount, args.OrderId, orderDatabase);
        }

        private void OnAddOrderMessageSlipPrinter(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleAddOrderMessage args, CargoProductPrototype product)
        {
            if (!_protoMan.Resolve(component.Account, out var account))
                return;

            if (Timing.CurTime < component.NextPrintTime)
                return;

            var label = Spawn(account.AcquisitionSlip, Transform(uid).Coordinates);
            component.NextPrintTime = Timing.CurTime + component.PrintDelay;
            _audio.PlayPvs(component.PrintSound, uid);

            var paper = EnsureComp<PaperComponent>(label);
            var msg = new FormattedMessage();

            msg.AddMarkupPermissive(Loc.GetString("cargo-acquisition-slip-body",
                ("product", product.Name),
                ("description", product.Description),
                ("unit", product.Cost),
                ("amount", args.Amount),
                ("cost", product.Cost * args.Amount),
                ("orderer", args.Requester),
                ("reason", args.Reason)));
            _paperSystem.SetContent((label, paper), msg.ToMarkup());

            var slip = EnsureComp<CargoSlipComponent>(label);
            slip.Product = product.ID;
            slip.Requester = args.Requester;
            slip.Reason = args.Reason;
            slip.OrderQuantity = args.Amount;
            slip.Account = component.Account;
        }

        private void OnAddOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleAddOrderMessage args)
        {
            if (args.Actor is not { Valid: true } player)
                return;

            if (args.Basket.Count <= 0)
                return;

            var stationUid = _station.GetOwningStation(uid);

            if (!TryGetOrderDatabase(stationUid, out var orderDatabase))
                return;

            if (!TryComp<StationBankAccountComponent>(stationUid, out var bank))
                return;

            // Invalid Order
            if (!IsInAvailableProducts((uid, component), args.Basket))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-invalid-product"));
                PlayDenySound(uid, component);
                return;
            }

            var order = new CargoOrderData(
                GenerateOrderId(orderDatabase),
                args.Basket,
                args.Requester,
                args.Reason,
                component.Account
            );

            if (!TryAddOrder(stationUid.Value, order, orderDatabase))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-too-many"));
                PlayDenySound(uid, component);
                return;
            }

            // Log order addition
            var adminString = "";
            foreach (var product in args.Basket)
            {
                adminString += $"{product.Quantity} {_protoMan.Index<CargoProductPrototype>(product.Product).Name},";
            }

            _adminLogger.Add(
                LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(player):user} added order [orderId:{order.OrderId}, products:{adminString} requester:{order.Requester}, reason:{order.Reason}]"
            );
        }

        private void OnOrderUIOpened(EntityUid uid, CargoOrderConsoleComponent component, BoundUIOpenedEvent args)
        {
            var station = _station.GetOwningStation(uid);
            UpdateOrderState(uid, station);
        }

        private void UpdateOrderState(EntityUid consoleUid, EntityUid? station)
        {
            if (!TryComp<CargoOrderConsoleComponent>(consoleUid, out var console))
                return;

            if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase))
                return;

            if (_uiSystem.HasUi(consoleUid, CargoConsoleUiKey.Orders))
            {
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
        }

        private void UpdateUndeliveredOrders(Entity<StationCargoOrderDatabaseComponent> ent)
        {
            if (!TryComp<StationDataComponent>(ent, out var stationData))
                return;

            var toDeliver = new List<CargoOrderData>();

            foreach (var order in ent.Comp.Orders)
            {
                if (!order.Approved)
                    continue;

                if (!order.Basket.Any(item => item.NumOrdered != item.Quantity))
                {
                    toDeliver.Add(order);
                    continue;
                }

                if (order.Assigned && TryGetEntity(order.AssignedEntity, out var _))
                    continue;

                if (TryExternalFulfillment((ent, stationData), order))
                    continue;

                if (
                    TryFulfillOrder((ent, stationData), order, ent.Comp)
                    && order.Basket.Any(item => item.NumOrdered != item.Quantity)
                )
                    toDeliver.Add(order);
            }

            foreach (var order in toDeliver)
                TryDeliverOrder(ent, order, ent.Comp);
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

        private List<CargoOrderData> RelevantOrders(
            Entity<StationCargoOrderDatabaseComponent> station,
            ProtoId<CargoAccountPrototype> account,
            bool? approved = null
        )
        {
            return RelevantOrders(station, station.Comp.Orders, account, approved);
        }

        /// <summary>
        /// Gets orders relevant to this account, i.e. orders on the account directly or orders on behalf of the account in the primary account.
        /// </summary>
        private List<CargoOrderData> RelevantOrders(
            Entity<StationCargoOrderDatabaseComponent> station,
            List<CargoOrderData> allOrders,
            ProtoId<CargoAccountPrototype> account,
            bool? approved = null
        )
        {
            if (!TryComp<StationBankAccountComponent>(station, out var bank))
                return [];

            IEnumerable<CargoOrderData> orders = allOrders.Where(order => order.Account == account);

            if (account == bank.PrimaryAccount)
                orders = allOrders;
            return [.. orders.Where(order => order.Visible && (approved == null || order.Approved == approved))];
        }

        private void ConsolePopup(EntityUid actor, string text)
        {
            _popup.PopupCursor(text, actor);
        }

        private void PlayDenySound(EntityUid uid, CargoOrderConsoleComponent component)
        {
            if (_timing.CurTime >= component.NextDenySoundTime)
            {
                component.NextDenySoundTime = _timing.CurTime + component.DenySoundDelay;
                _audio.PlayPvs(_audio.ResolveSound(component.ErrorSound), uid);
            }
        }

        public int GetOutstandingOrderCount(Entity<StationCargoOrderDatabaseComponent> station, ProtoId<CargoAccountPrototype> account)
        {
            return RelevantOrders(station, account, false).Count;
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

        public bool AddAndApproveOrder(
            EntityUid dbUid,
            List<CargoOrderItemData> basket,
            string sender,
            string description,
            string dest,
            StationCargoOrderDatabaseComponent orderDatabase,
            ProtoId<CargoAccountPrototype> account,
            Entity<StationDataComponent> stationData
        )
        {
            // Make an order
            var order = new CargoOrderData(GenerateOrderId(orderDatabase), basket, sender, description, account);
            order.Visible = false;

            // Approve it now
            order.SetApproverData(dest, sender);
            order.Approved = true;

            // Log order addition
            var adminString = "";
            foreach (var product in order.Basket)
            {
                if (!_protoMan.TryIndex<CargoProductPrototype>(product.Product, out var productProto))
                    continue;
                adminString += $"{product.Quantity} {productProto.Name},";
            }

            _adminLogger.Add(
                LogType.Action,
                LogImpact.Low,
                $"AddAndApproveOrder {description} added order [orderId:{order.OrderId}, products:{adminString} requester:{order.Requester}, reason:{order.Reason}]"
            );

            // Add it to the list
            return TryAddOrder(dbUid, order, orderDatabase) && TryFulfillOrder(stationData, order, orderDatabase);
        }

        private bool TryAddOrder(
            EntityUid dbUid,
            CargoOrderData order,
            StationCargoOrderDatabaseComponent orderDatabase
        )
        {
            var outstanding = GetOutstandingOrderCount((dbUid, orderDatabase), order.Account);
            if (outstanding >= orderDatabase.Capacity)
            {
                return false;
            }
            orderDatabase.Orders.Add(order);
            UpdateOrders(dbUid);
            return true;
        }

        private bool TryDeliverOrder(
            EntityUid dbUid,
            CargoOrderData order,
            StationCargoOrderDatabaseComponent orderDatabase
        )
        {
            orderDatabase.Orders.Remove(order);
            orderDatabase.DeliveredOrders.Add(order);
            // Prevent unbounded growth of delivered orders.
            if (orderDatabase.DeliveredOrders.Count > 1000)
                orderDatabase.DeliveredOrders.RemoveAt(0);
            UpdateOrders(dbUid);
            return true;
        }

        private static int GenerateOrderId(StationCargoOrderDatabaseComponent orderDB)
        {
            // We need an arbitrary unique ID to identify orders, since they may
            // want to be cancelled later.
            return ++orderDB.NumOrdersCreated;
        }

        public void RemoveOrder(EntityUid dbUid, int index, StationCargoOrderDatabaseComponent orderDatabase)
        {
            var sequenceIdx = orderDatabase.Orders.FindIndex(order => order.OrderId == index);
            if (sequenceIdx != -1)
            {
                orderDatabase.Orders.RemoveAt(sequenceIdx);
            }
            UpdateOrders(dbUid);
        }

        public void ClearOrders(StationCargoOrderDatabaseComponent orderDatabase)
        {
            if (orderDatabase.Orders.Count == 0)
                return;

            orderDatabase.Orders.Clear();
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

            if (container.IsSingleProduct || container.Container == "")
            {
                if (!_protoMan.TryIndex<CargoProductPrototype>(container.Products.First().Product, out var singleProto))
                    return false;
                containerEntity = Spawn(singleProto.SpawnList.First(), spawn);
                container.Products.First().NumOrdered += 1;
            }
            else
            {
                containerEntity = Spawn(container.Container, spawn);
            }

            if (!containerEntity.IsValid())
                return false;

            _transformSystem.Unanchor(containerEntity, Transform(containerEntity));

            if (!container.IsSingleProduct)
            {
                foreach (var item in container.Products)
                {
                    if (!_protoMan.TryIndex<CargoProductPrototype>(item.Product, out var productProto))
                        return false;
                    for (int i = 0; i < item.Quantity; i++)
                    {
                        foreach (var product in productProto.SpawnList)
                        {
                            var itemEntity = Spawn(product, spawn);
                            if (
                                !_container.TryGetContainer(containerEntity, container.ContainerID, out var container1)
                                || !_container.Insert(itemEntity, container1, force: true)
                            )
                            {
                                DebugTools.Assert(
                                    $"Failed to insert cargo product into its specified container. This indicates an error in the cargo product definition's YAML as the product should be insertable into its container. {productProto.Name}: {(EntProtoId)container.Container}"
                                );
                            }
                        }
                        item.NumOrdered++;
                    }
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

            ApplyLabelsAndWrapSingletons(containers, order);
            return containers;
        }

        private void ApplyLabelsAndWrapSingletons(List<CargoOrderContainerData> containers, CargoOrderData order)
        {
            foreach (var container in containers)
            {
                container.LabelMessage = GetContainerLabel(container, order);
                container.LabelName = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.OrderId));
            }
        }

        /// <summary>
        /// Count the number of items which will be spawned in a container
        /// </summary>
        /// <summary>
        /// Create the string which will go on the label of the container
        /// </summary>
        private string GetContainerLabel(CargoOrderContainerData container, CargoOrderData order)
        {
            var accountProto = _protoMan.Index(order.Account);
            string message;
            if (container.IsSingleProduct)
            {
                if (!_protoMan.TryIndex<CargoProductPrototype>(container.Products[0].Product, out var singleProto))
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
            }
            else
            {
                message = Loc.GetString("cargo-console-paper-print-header", ("orderNumber", order.OrderId));
                message += "\n";
                foreach (var product in container.Products)
                {
                    if (!_protoMan.TryIndex<CargoProductPrototype>(product.Product, out var productProto))
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
            }
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
                if (!_protoMan.TryIndex<CargoProductPrototype>(product.Product, out var _))
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

        public List<ProtoId<CargoProductPrototype>> GetAvailableProducts(Entity<CargoOrderConsoleComponent> ent)
        {
            if (
                _station.GetOwningStation(ent) is not { } station
                || !TryComp<StationCargoOrderDatabaseComponent>(station, out var db)
            )
            {
                return new List<ProtoId<CargoProductPrototype>>();
            }

            var products = new List<ProtoId<CargoProductPrototype>>();

            // Note that a market must be both on the station and on the console to be available.
            var markets = ent.Comp.AllowedGroups.Intersect(db.Markets).ToList();
            foreach (var product in _protoMan.EnumeratePrototypes<CargoProductPrototype>())
            {
                if (!markets.Contains(product.Group))
                    continue;

                products.Add(product.ID);
            }

            return products;
        }

        private bool TryGetOrderDatabase(
            [NotNullWhen(true)] EntityUid? stationUid,
            [MaybeNullWhen(false)] out StationCargoOrderDatabaseComponent dbComp
        )
        {
            return TryComp(stationUid, out dbComp);
        }
    }
}
