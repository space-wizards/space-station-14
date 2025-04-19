using System.Diagnostics.CodeAnalysis;
using Content.Server.Cargo.Components;
using Content.Server.Station.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Paper;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems
{
    public sealed partial class CargoSystem
    {
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly EmagSystem _emag = default!;

        private void InitializeConsole()
        {
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleAddOrderMessage>(OnAddOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleRemoveOrderMessage>(OnRemoveOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleApproveOrderMessage>(OnApproveOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, BoundUIOpenedEvent>(OnOrderUIOpened);
            SubscribeLocalEvent<CargoOrderConsoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<CargoOrderConsoleComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<CargoOrderConsoleComponent, GotEmaggedEvent>(OnEmagged);
        }

        private void OnInteractUsing(EntityUid uid, CargoOrderConsoleComponent component, ref InteractUsingEvent args)
        {
            if (!HasComp<CashComponent>(args.Used))
                return;

            var price = _pricing.GetPrice(args.Used);

            if (price == 0)
                return;

            var stationUid = _station.GetOwningStation(args.Used);

            if (!TryComp(stationUid, out StationBankAccountComponent? bank))
                return;

            _audio.PlayPvs(ApproveSound, uid);
            UpdateBankAccount((stationUid.Value, bank), (int) price, component.Account);
            QueueDel(args.Used);
            args.Handled = true;
        }

        private void OnInit(EntityUid uid, CargoOrderConsoleComponent orderConsole, ComponentInit args)
        {
            var station = _station.GetOwningStation(uid);
            UpdateOrderState(uid, station);
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
            var stationQuery = EntityQueryEnumerator<StationBankAccountComponent>();
            while (stationQuery.MoveNext(out var uid, out var bank))
            {
                if (Timing.CurTime < bank.NextIncomeTime)
                    continue;
                bank.NextIncomeTime += bank.IncomeDelay;

                var balanceToAdd = (int) Math.Round(bank.IncreasePerSecond * bank.IncomeDelay.TotalSeconds);
                UpdateBankAccount((uid, bank), balanceToAdd, bank.RevenueDistribution);
            }
        }

        #region Interface

        private void OnApproveOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleApproveOrderMessage args)
        {
            if (args.Actor is not { Valid: true } player)
                return;

            if (!_accessReaderSystem.IsAllowed(player, uid))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
                PlayDenySound(uid, component);
                return;
            }

            var station = _station.GetOwningStation(uid);

            // No station to deduct from.
            if (!TryComp(station, out StationBankAccountComponent? bank) ||
                !TryComp(station, out StationDataComponent? stationData) ||
                !TryGetOrderDatabase(station, out var orderDatabase))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-station-not-found"));
                PlayDenySound(uid, component);
                return;
            }

            // Find our order again. It might have been dispatched or approved already
            var order = orderDatabase.Orders[component.Account].Find(order => args.OrderId == order.OrderId && !order.Approved);
            if (order == null)
            {
                return;
            }

            // Invalid order
            if (!_protoMan.HasIndex<EntityPrototype>(order.ProductId))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-invalid-product"));
                PlayDenySound(uid, component);
                return;
            }

            var amount = GetOutstandingOrderCount(orderDatabase, component.Account);
            var capacity = orderDatabase.Capacity;

            // Too many orders, avoid them getting spammed in the UI.
            if (amount >= capacity)
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-too-many"));
                PlayDenySound(uid, component);
                return;
            }

            // Cap orders so someone can't spam thousands.
            var cappedAmount = Math.Min(capacity - amount, order.OrderQuantity);

            if (cappedAmount != order.OrderQuantity)
            {
                order.OrderQuantity = cappedAmount;
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-snip-snip"));
                PlayDenySound(uid, component);
            }

            var cost = order.Price * order.OrderQuantity;
            var accountBalance = GetBalanceFromAccount((station.Value, bank), component.Account);

            // Not enough balance
            if (cost > accountBalance)
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)));
                PlayDenySound(uid, component);
                return;
            }

            var ev = new FulfillCargoOrderEvent((station.Value, stationData), order, (uid, component));
            RaiseLocalEvent(ref ev);
            ev.FulfillmentEntity ??= station.Value;

            if (!ev.Handled)
            {
                ev.FulfillmentEntity = TryFulfillOrder((station.Value, stationData), component.Account, order, orderDatabase);

                if (ev.FulfillmentEntity == null)
                {
                    ConsolePopup(args.Actor, Loc.GetString("cargo-console-unfulfilled"));
                    PlayDenySound(uid, component);
                    return;
                }
            }

            order.Approved = true;
            _audio.PlayPvs(ApproveSound, uid);

            if (!_emag.CheckFlag(uid, EmagType.Interaction))
            {
                var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(uid, player);
                RaiseLocalEvent(tryGetIdentityShortInfoEvent);
                order.SetApproverData(tryGetIdentityShortInfoEvent.Title);

                var message = Loc.GetString("cargo-console-unlock-approved-order-broadcast",
                    ("productName", Loc.GetString(order.ProductName)),
                    ("orderAmount", order.OrderQuantity),
                    ("approver", order.Approver ?? string.Empty),
                    ("cost", cost));
                _radio.SendRadioMessage(uid, message, component.AnnouncementChannel, uid, escapeMarkup: false);
                if (CargoOrderConsoleComponent.BaseAnnouncementChannel != component.AnnouncementChannel)
                    _radio.SendRadioMessage(uid, message, CargoOrderConsoleComponent.BaseAnnouncementChannel, uid, escapeMarkup: false);
            }

            ConsolePopup(args.Actor, Loc.GetString("cargo-console-trade-station", ("destination", MetaData(ev.FulfillmentEntity.Value).EntityName)));

            // Log order approval
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(player):user} approved order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.ProductId}, requester:{order.Requester}, reason:{order.Reason}] on account {component.Account} with balance at {accountBalance}");

            orderDatabase.Orders[component.Account].Remove(order);
            UpdateBankAccount((station.Value, bank), -cost, component.Account);
            UpdateOrders(station.Value);
        }

        private EntityUid? TryFulfillOrder(Entity<StationDataComponent> stationData, ProtoId<CargoAccountPrototype> account, CargoOrderData order, StationCargoOrderDatabaseComponent orderDatabase)
        {
            // No slots at the trade station
            _listEnts.Clear();
            GetTradeStations(stationData, ref _listEnts);
            EntityUid? tradeDestination = null;

            // Try to fulfill from any station where possible, if the pad is not occupied.
            foreach (var trade in _listEnts)
            {
                var tradePads = GetCargoPallets(trade, BuySellType.Buy);
                _random.Shuffle(tradePads);

                var freePads = GetFreeCargoPallets(trade, tradePads);
                if (freePads.Count >= order.OrderQuantity) //check if the station has enough free pallets
                {
                    foreach (var pad in freePads)
                    {
                        var coordinates = new EntityCoordinates(trade, pad.Transform.LocalPosition);

                        if (FulfillOrder(order, account, coordinates, orderDatabase.PrinterOutput))
                        {
                            tradeDestination = trade;
                            order.NumDispatched++;
                            if (order.OrderQuantity <= order.NumDispatched) //Spawn a crate on free pellets until the order is fulfilled.
                                break;
                        }
                    }
                }

                if (tradeDestination != null)
                    break;
            }

            return tradeDestination;
        }

        private void GetTradeStations(StationDataComponent data, ref List<EntityUid> ents)
        {
            foreach (var gridUid in data.Grids)
            {
                if (!_tradeQuery.HasComponent(gridUid))
                    continue;

                ents.Add(gridUid);
            }
        }

        private void OnRemoveOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleRemoveOrderMessage args)
        {
            var station = _station.GetOwningStation(uid);

            if (!TryGetOrderDatabase(station, out var orderDatabase))
                return;

            RemoveOrder(station.Value, component.Account, args.OrderId, orderDatabase);
        }

        private void OnAddOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleAddOrderMessage args)
        {
            if (args.Actor is not { Valid: true } player)
                return;

            if (args.Amount <= 0)
                return;

            var stationUid = _station.GetOwningStation(uid);

            if (!TryGetOrderDatabase(stationUid, out var orderDatabase))
                return;

            if (!_protoMan.TryIndex<CargoProductPrototype>(args.CargoProductId, out var product))
            {
                Log.Error($"Tried to add invalid cargo product {args.CargoProductId} as order!");
                return;
            }

            if (!component.AllowedGroups.Contains(product.Group))
                return;

            var data = GetOrderData(args, product, GenerateOrderId(orderDatabase));

            if (!TryAddOrder(stationUid.Value, component.Account, data, orderDatabase))
            {
                PlayDenySound(uid, component);
                return;
            }

            // Log order addition
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(player):user} added order [orderId:{data.OrderId}, quantity:{data.OrderQuantity}, product:{data.ProductId}, requester:{data.Requester}, reason:{data.Reason}]");

        }

        private void OnOrderUIOpened(EntityUid uid, CargoOrderConsoleComponent component, BoundUIOpenedEvent args)
        {
            var station = _station.GetOwningStation(uid);
            UpdateOrderState(uid, station);
        }

        #endregion

        private void UpdateOrderState(EntityUid consoleUid, EntityUid? station)
        {
            if (!TryComp<CargoOrderConsoleComponent>(consoleUid, out var console))
                return;

            if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase))
                return;

            if (_uiSystem.HasUi(consoleUid, CargoConsoleUiKey.Orders))
            {
                _uiSystem.SetUiState(consoleUid,
                    CargoConsoleUiKey.Orders,
                    new CargoConsoleInterfaceState(
                    MetaData(station.Value).EntityName,
                    GetOutstandingOrderCount(orderDatabase, console.Account),
                    orderDatabase.Capacity,
                    GetNetEntity(station.Value),
                    orderDatabase.Orders[console.Account]
                ));
            }
        }

        private void ConsolePopup(EntityUid actor, string text)
        {
            _popup.PopupCursor(text, actor);
        }

        private void PlayDenySound(EntityUid uid, CargoOrderConsoleComponent component)
        {
            _audio.PlayPvs(_audio.ResolveSound(component.ErrorSound), uid);
        }

        private static CargoOrderData GetOrderData(CargoConsoleAddOrderMessage args, CargoProductPrototype cargoProduct, int id)
        {
            return new CargoOrderData(id, cargoProduct.Product, cargoProduct.Name, cargoProduct.Cost, args.Amount, args.Requester, args.Reason);
        }

        public static int GetOutstandingOrderCount(StationCargoOrderDatabaseComponent component, ProtoId<CargoAccountPrototype> account)
        {
            var amount = 0;

            foreach (var order in component.Orders[account])
            {
                if (!order.Approved)
                    continue;
                amount += order.OrderQuantity - order.NumDispatched;
            }

            return amount;
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

            var consoleQuery = AllEntityQuery<CargoShuttleConsoleComponent>();
            while (consoleQuery.MoveNext(out var uid, out var _))
            {
                var station = _station.GetOwningStation(uid);
                if (station != dbUid)
                    continue;

                UpdateShuttleState(uid, station);
            }
        }

        public bool AddAndApproveOrder(
            EntityUid dbUid,
            string spawnId,
            string name,
            int cost,
            int qty,
            string sender,
            string description,
            string dest,
            StationCargoOrderDatabaseComponent component,
            ProtoId<CargoAccountPrototype> account,
            Entity<StationDataComponent> stationData
        )
        {
            DebugTools.Assert(_protoMan.HasIndex<EntityPrototype>(spawnId));
            // Make an order
            var id = GenerateOrderId(component);
            var order = new CargoOrderData(id, spawnId, name, cost, qty, sender, description);

            // Approve it now
            order.SetApproverData(dest, sender);
            order.Approved = true;

            // Log order addition
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"AddAndApproveOrder {description} added order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.ProductId}, requester:{order.Requester}, reason:{order.Reason}]");

            // Add it to the list
            return TryAddOrder(dbUid, account, order, component) && TryFulfillOrder(stationData, account, order, component).HasValue;
        }

        private bool TryAddOrder(EntityUid dbUid, ProtoId<CargoAccountPrototype> account, CargoOrderData data, StationCargoOrderDatabaseComponent component)
        {
            component.Orders[account].Add(data);
            UpdateOrders(dbUid);
            return true;
        }

        private static int GenerateOrderId(StationCargoOrderDatabaseComponent orderDB)
        {
            // We need an arbitrary unique ID to identify orders, since they may
            // want to be cancelled later.
            return ++orderDB.NumOrdersCreated;
        }

        public void RemoveOrder(EntityUid dbUid, ProtoId<CargoAccountPrototype> account, int index, StationCargoOrderDatabaseComponent orderDB)
        {
            var sequenceIdx = orderDB.Orders[account].FindIndex(order => order.OrderId == index);
            if (sequenceIdx != -1)
            {
                orderDB.Orders[account].RemoveAt(sequenceIdx);
            }
            UpdateOrders(dbUid);
        }

        public void ClearOrders(StationCargoOrderDatabaseComponent component)
        {
            if (component.Orders.Count == 0)
                return;

            component.Orders.Clear();
        }

        private static bool PopFrontOrder(StationCargoOrderDatabaseComponent orderDB, ProtoId<CargoAccountPrototype> account, [NotNullWhen(true)] out CargoOrderData? orderOut)
        {
            var orderIdx = orderDB.Orders[account].FindIndex(order => order.Approved);
            if (orderIdx == -1)
            {
                orderOut = null;
                return false;
            }

            orderOut = orderDB.Orders[account][orderIdx];
            orderOut.NumDispatched++;

            if (orderOut.NumDispatched >= orderOut.OrderQuantity)
            {
                // Order is complete. Remove from the queue.
                orderDB.Orders[account].RemoveAt(orderIdx);
            }
            return true;
        }

        /// <summary>
        /// Tries to fulfill the next outstanding order.
        /// </summary>
        [PublicAPI]
        private bool FulfillNextOrder(StationCargoOrderDatabaseComponent orderDB, ProtoId<CargoAccountPrototype> account, EntityCoordinates spawn, string? paperProto)
        {
            if (!PopFrontOrder(orderDB, account, out var order))
                return false;

            return FulfillOrder(order, account, spawn, paperProto);
        }

        /// <summary>
        /// Fulfills the specified cargo order and spawns paper attached to it.
        /// </summary>
        private bool FulfillOrder(CargoOrderData order, ProtoId<CargoAccountPrototype> account, EntityCoordinates spawn, string? paperProto)
        {
            // Create the item itself
            var item = Spawn(order.ProductId, spawn);

            // Ensure the item doesn't start anchored
            _transformSystem.Unanchor(item, Transform(item));

            // Create a sheet of paper to write the order details on
            var printed = EntityManager.SpawnEntity(paperProto, spawn);
            if (TryComp<PaperComponent>(printed, out var paper))
            {
                // fill in the order data
                var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.OrderId));
                _metaSystem.SetEntityName(printed, val);

                var accountProto = _protoMan.Index(account);
                _paperSystem.SetContent((printed, paper),
                    Loc.GetString(
                        "cargo-console-paper-print-text",
                        ("orderNumber", order.OrderId),
                        ("itemName", MetaData(item).EntityName),
                        ("orderQuantity", order.OrderQuantity),
                        ("requester", order.Requester),
                        ("reason", string.IsNullOrWhiteSpace(order.Reason) ? Loc.GetString("cargo-console-paper-reason-default") : order.Reason),
                        ("account", Loc.GetString(accountProto.Name)),
                        ("accountcode", Loc.GetString(accountProto.Code)),
                        ("approver", string.IsNullOrWhiteSpace(order.Approver) ? Loc.GetString("cargo-console-paper-approver-default") : order.Approver)));

                // attempt to attach the label to the item
                if (TryComp<PaperLabelComponent>(item, out var label))
                {
                    _slots.TryInsert(item, label.LabelSlot, printed, null);
                }
            }

            return true;

        }

        #region Station

        private bool TryGetOrderDatabase([NotNullWhen(true)] EntityUid? stationUid, [MaybeNullWhen(false)] out StationCargoOrderDatabaseComponent dbComp)
        {
            return TryComp(stationUid, out dbComp);
        }

        #endregion
    }
}
