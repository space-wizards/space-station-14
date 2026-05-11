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
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Paper;
using Content.Shared.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Random;

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

        private void OnApproveOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleApproveOrderMessage args)
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
            if (!TryComp(station, out StationBankAccountComponent? bank) ||
                !TryComp(station, out StationDataComponent? stationData) ||
                !TryGetOrderDatabase(station, out var orderDatabase))
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
            if (!_protoMan.Resolve(order.Product, out var product))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-invalid-product"));
                PlayDenySound(uid, component);
                return;
            }

            var amount = GetOutstandingOrderCount((station.Value, orderDatabase), order.Account);
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

            var cost = product.Cost * order.OrderQuantity;
            var accountBalance = GetBalanceFromAccount((station.Value, bank), order.Account);

            // Not enough balance
            if (cost > accountBalance)
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)));
                PlayDenySound(uid, component);
                return;
            }

            order.Approved = true;
            _audio.PlayPvs(ApproveSound, uid);

            if (!_emag.CheckFlag(uid, EmagType.Interaction))
            {
                var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(uid, player);
                RaiseLocalEvent(tryGetIdentityShortInfoEvent);
                order.SetApproverData(tryGetIdentityShortInfoEvent.Title);

                var message = Loc.GetString("cargo-console-unlock-approved-order-broadcast",
                    ("productName", Loc.GetString(product.Name)),
                    ("orderAmount", order.OrderQuantity),
                    ("approver", order.Approver ?? string.Empty),
                    ("cost", cost));
                _radio.SendRadioMessage(uid, message, account.RadioChannel, uid, escapeMarkup: false);
                if (CargoOrderConsoleComponent.BaseAnnouncementChannel != account.RadioChannel)
                    _radio.SendRadioMessage(uid, message, CargoOrderConsoleComponent.BaseAnnouncementChannel, uid, escapeMarkup: false);
            }

            // Log order approval
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(player):user} approved order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.Product}, requester:{order.Requester}, reason:{order.Reason}] on account {order.Account} with balance at {accountBalance}");

            UpdateBankAccount((station.Value, bank), -cost, order.Account);
            UpdateOrders(station.Value);
            UpdateUndeliveredOrders((station.Value, orderDatabase));
        }

        private bool TryFulfillOrder(Entity<StationDataComponent> stationData, CargoOrderData order, StationCargoOrderDatabaseComponent orderDatabase)
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
                foreach (var pad in freePads)
                {
                    var coordinates = new EntityCoordinates(trade, pad.Transform.LocalPosition);

                    if (FulfillOrder(order, coordinates, orderDatabase.PrinterOutput))
                    {
                        tradeDestination = trade;
                        order.NumDispatched++;
                        if (order.OrderQuantity <= order.NumDispatched) //Spawn a crate on free pellets until the order is fulfilled.
                            break;
                    }
                }

                if (tradeDestination != null)
                    break;
            }

            return tradeDestination.HasValue;
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

            if (!TryComp<StationBankAccountComponent>(stationUid, out var bank))
                return;

            if (!_protoMan.TryIndex<CargoProductPrototype>(args.CargoProductId, out var product))
            {
                Log.Error($"Tried to add invalid cargo product {args.CargoProductId} as order!");
                return;
            }

            if (!GetAvailableProducts((uid, component)).Contains(args.CargoProductId))
                return;

            var targetAccount = component.Mode == CargoOrderConsoleMode.SendToPrimary ? bank.PrimaryAccount : component.Account;

            var order = new CargoOrderData(GenerateOrderId(orderDatabase), args.CargoProductId, args.Amount, args.Requester, args.Reason, targetAccount);

            if (!TryAddOrder(stationUid.Value, order, orderDatabase))
            {
                PlayDenySound(uid, component);
                return;
            }

            // Log order addition
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(player):user} added order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order}, requester:{order.Requester}, reason:{order.Reason}]");

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
                var orderHistory = orderDatabase.DeliveredOrders.Concat(orderDatabase.Orders).OrderBy(order => order.OrderId).ToList();
                _uiSystem.SetUiState(consoleUid,
                    CargoConsoleUiKey.Orders,
                    new CargoConsoleInterfaceState(
                    MetaData(station.Value).EntityName,
                    GetOutstandingOrderCount((station!.Value, orderDatabase), console.Account),
                    orderDatabase.Capacity,
                    GetNetEntity(station.Value),
                    RelevantOrders((station!.Value, orderDatabase), orderDatabase.Orders, console.Account, approved: false),
                    GetAvailableProducts((consoleUid, console))
                ));
            }
        }
        private void UpdateUndeliveredOrders(Entity<StationCargoOrderDatabaseComponent> ent)
        {
            if (!TryComp<StationDataComponent>(ent, out var stationData))
                return;

            var toDeliver = new List<CargoOrderData>();

            foreach (var order in ent.Comp.Orders.Where(order => order.Approved))
            {
                if (order.NumDispatched >= order.OrderQuantity)
                {
                    toDeliver.Add(order);
                    continue;
                }

                if (order.Assigned)
                    continue;

                var ev = new FulfillCargoOrderEvent((ent, stationData), order);
                RaiseLocalEvent(ref ev);
                if (ev.Handled)
                {
                    order.Assigned = true;
                    continue;
                }

                if (TryFulfillOrder((ent, stationData), order, ent.Comp) && order.NumDispatched >= order.OrderQuantity)
                    toDeliver.Add(order);
            }

            foreach (var order in toDeliver)
                TryDeliveredOrder(ent, order, ent.Comp);
        }

        private List<CargoOrderData> RelevantOrders(Entity<StationCargoOrderDatabaseComponent> station, ProtoId<CargoAccountPrototype> account, bool approved = false)
        {
            return RelevantOrders(station, station.Comp.Orders, account, approved);
        }
        /// <summary>
        /// Gets orders relevant to this account, i.e. orders on the account directly or orders on behalf of the account in the primary account.
        /// </summary>
        private List<CargoOrderData> RelevantOrders(Entity<StationCargoOrderDatabaseComponent> station, List<CargoOrderData> allOrders, ProtoId<CargoAccountPrototype> account, bool approved = false)
        {
            if (!TryComp<StationBankAccountComponent>(station, out var bank))
                return [];

            IEnumerable<CargoOrderData> orders = allOrders.Where(order => order.Account == account);

            if (account == bank.PrimaryAccount)
                orders = allOrders;
            return [.. orders.Where(order => order.Approved == approved)];
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
            return RelevantOrders(station, account, false).Count + RelevantOrders(station, account, true).Sum(order => order.OrderQuantity - order.NumDispatched);
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
            CargoProductPrototype product,
            int qty,
            string sender,
            string description,
            string dest,
            StationCargoOrderDatabaseComponent orderDatabase,
            ProtoId<CargoAccountPrototype> account,
            Entity<StationDataComponent> stationData
        )
        {
            // Make an order
            var id = GenerateOrderId(orderDatabase);
            var order = new CargoOrderData(id, product, qty, sender, description, account);

            // Approve it now
            order.SetApproverData(dest, sender);
            order.Approved = true;

            // Log order addition
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"AddAndApproveOrder {description} added order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.Product}, requester:{order.Requester}, reason:{order.Reason}]");

            // Add it to the list
            return TryAddOrder(dbUid, order, orderDatabase) && TryFulfillOrder(stationData, order, orderDatabase);
        }

        private bool TryAddOrder(EntityUid dbUid, CargoOrderData order, StationCargoOrderDatabaseComponent orderDatabase)
        {
            orderDatabase.Orders.Add(order);
            UpdateOrders(dbUid);
            return true;
        }
        private bool TryDeliveredOrder(EntityUid dbUid, CargoOrderData order, StationCargoOrderDatabaseComponent orderDatabase)
        {
            orderDatabase.Orders.Remove(order);
            orderDatabase.DeliveredOrders.Add(order);
            UpdateOrders(dbUid);
            return true;
        }
        private static int GenerateOrderId(StationCargoOrderDatabaseComponent orderDB)
        {
            // We need an arbitrary unique ID to identify orders, since they may
            // want to be cancelled later.
            return ++orderDB.NumOrdersCreated;
        }

        public void RemoveOrder(EntityUid dbUid, ProtoId<CargoAccountPrototype> account, int index, StationCargoOrderDatabaseComponent orderDatabase)
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
        private bool FulfillOrder(CargoOrderData order, EntityCoordinates spawn, string? paperProto)
        {
            if (!_protoMan.Resolve(order.Product, out var product))
                return false;

            // Create the item itself
            var item = Spawn(product.Product, spawn);
            var itemXForm = Transform(item);

            // Ensure the item doesn't start anchored
            _transformSystem.Unanchor(item, itemXForm);

            // Spawn container and insert the item into it if a container is defined.
            if (product.Container is { } productContainer)
            {
                var containerEntity = Spawn(productContainer.Entity, itemXForm.Coordinates);
                _transformSystem.SetLocalRotation(containerEntity, itemXForm.LocalRotation);

                if (!_container.TryGetContainer(containerEntity, productContainer.ContainerId, out var container1) ||
                    !_container.Insert(item, container1, force: true))
                {
                    DebugTools.Assert(
                        $"Failed to insert cargo product into its specified container. This indicates an error in the cargo product definition's YAML as the product should be insertable into its container. {nameof(CargoProductPrototype)}: {(ProtoId<CargoProductPrototype>)order.Product.Id}");
                    QueueDel(containerEntity);
                }
                else
                {
                    item = containerEntity;
                }
            }

            // Create a sheet of paper to write the order details on
            var printed = Spawn(paperProto, spawn);
            if (TryComp<PaperComponent>(printed, out var paper))
            {
                // fill in the order data
                var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.OrderId));
                _metaSystem.SetEntityName(printed, val);

                var accountProto = _protoMan.Index(order.Account);
                _paperSystem.SetContent((printed, paper),
                    Loc.GetString(
                        "cargo-console-paper-print-text",
                        ("orderNumber", order.OrderId),
                        ("itemName", product.Name),
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

        public List<ProtoId<CargoProductPrototype>> GetAvailableProducts(Entity<CargoOrderConsoleComponent> ent)
        {
            if (_station.GetOwningStation(ent) is not { } station ||
                !TryComp<StationCargoOrderDatabaseComponent>(station, out var db))
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

        #region Station

        private bool TryGetOrderDatabase([NotNullWhen(true)] EntityUid? stationUid, [MaybeNullWhen(false)] out StationCargoOrderDatabaseComponent dbComp)
        {
            return TryComp(stationUid, out dbComp);
        }

        #endregion
    }
}
