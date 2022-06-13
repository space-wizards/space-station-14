using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Cargo.Components;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.System;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Content.Server.Cargo.Systems
{
    public sealed partial class CargoSystem
    {
        /// <summary>
        /// How much time to wait (in seconds) before increasing bank accounts balance.
        /// </summary>
        private const float Delay = 10f;

        /// <summary>
        /// How many points to give to every bank account every <see cref="Delay"/> seconds.
        /// </summary>
        private const int PointIncrease = 150;

        /// <summary>
        /// Keeps track of how much time has elapsed since last balance increase.
        /// </summary>
        private float _timer;

        // TODO: Move the station stuff to server

        [Dependency] private readonly IdCardSystem _idCardSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
        [Dependency] private readonly SignalLinkerSystem _linker = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        private void InitializeConsole()
        {
            SubscribeLocalEvent<CargoConsoleComponent, CargoConsoleAddOrderMessage>(OnAddOrderMessage);
            SubscribeLocalEvent<CargoConsoleComponent, CargoConsoleRemoveOrderMessage>(OnRemoveOrderMessage);
            SubscribeLocalEvent<CargoConsoleComponent, CargoConsoleApproveOrderMessage>(OnApproveOrderMessage);
            SubscribeLocalEvent<CargoConsoleComponent, CargoConsoleShuttleMessage>(OnShuttleMessage);
            SubscribeLocalEvent<CargoConsoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            Reset();
        }


        private void OnInit(EntityUid uid, CargoConsoleComponent console, ComponentInit args)
        {
            _linker.EnsureTransmitterPorts(uid, console.SenderPort);

            var station = Get<StationSystem>().GetOwningStation(uid);

            UpdateUIState(console, station);
        }

        private void Reset(RoundRestartCleanupEvent ev)
        {
            Reset();
        }

        private void Reset()
        {
            _timer = 0;
        }

        private void UpdateConsole(float frameTime)
        {
            _timer += frameTime;

            while (_timer > Delay)
            {
                _timer -= Delay;

                foreach (var account in EntityQuery<StationBankAccountComponent>())
                {
                    account.Balance += PointIncrease;
                }

                foreach (var comp in EntityQuery<CargoConsoleComponent>())
                {
                    var station = Get<StationSystem>().GetOwningStation(comp.Owner);

                    UpdateUIState(comp, station);
                }
            }
        }

        #region Interface

        private void OnShuttleMessage(EntityUid uid, CargoConsoleComponent component, CargoConsoleShuttleMessage args)
        {
            // TODO: Move this to CargoCOnsoleTelepadSystem or whatever.

            // Jesus fucking christ Glass
            //var approvedOrders = _cargoOrderDataManager.RemoveAndGetApprovedFrom(orders.Database);
            //orders.Database.ClearOrderCapacity();

            // TODO replace with shuttle code
            EntityUid? cargoTelepad = null;

            if (TryComp<SignalTransmitterComponent>(uid, out var transmitter) &&
                transmitter.Outputs.TryGetValue(component.SenderPort, out var telepad) &&
                telepad.Count > 0)
            {
                // use most recent link
                var pad = telepad[^1].Uid;
                if (HasComp<CargoConsoleTelepadComponent>(pad) &&
                    TryComp<ApcPowerReceiverComponent?>(pad, out var powerReceiver) &&
                    powerReceiver.Powered)
                    cargoTelepad = pad;
            }

            /*
            if (cargoTelepad != null)
            {
                if (TryComp<CargoConsoleTelepadComponent?>(cargoTelepad.Value, out var telepadComponent))
                {
                    var approvedOrders = _cargoConsoleSystem.RemoveAndGetApprovedOrders(orders.Database.Id);
                    orders.Database.ClearOrderCapacity();
                    foreach (var order in approvedOrders)
                    {
                        _cargoConsoleSystem.QueueTeleport(telepadComponent, order);
                    }
                }
            }
            */
        }

        private void OnApproveOrderMessage(EntityUid uid, CargoConsoleComponent component, CargoConsoleApproveOrderMessage args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} player)
                return;

            var orderDatabase = GetOrderDatabase(component);
            var bankAccount = GetBankAccount(component);

            // No station to deduct from.
            if (orderDatabase == null || bankAccount == null)
            {
                ConsolePopup(args.Session, "No available station");
                PlayDenySound(uid, component);
                return;
            }

            // No order to approve?
            if (!orderDatabase.Orders.TryGetValue(args.OrderNumber, out var order) ||
                order.Approved) return;

            // Invalid order
            if (!_protoMan.TryIndex<CargoProductPrototype>(order.ProductId, out var product))
            {
                ConsolePopup(args.Session, "Invalid product ID");
                PlayDenySound(uid, component);
                return;
            }

            var amount = GetOrderCount(orderDatabase);
            var capacity = orderDatabase.Capacity;

            // Too many orders, avoid them getting spammed in the UI.
            if (amount >= capacity)
            {
                ConsolePopup(args.Session, "Too many approved orders");
                PlayDenySound(uid, component);
                return;
            }

            // Cap orders so someone can't spam thousands.
            var orderAmount = Math.Min(capacity - amount, order.Amount);

            if (orderAmount != order.Amount)
            {
                order.Amount = orderAmount;
                ConsolePopup(args.Session, "Order trimmed to capacity");
                PlayDenySound(uid, component);
            }

            var cost = product.PointCost * order.Amount;

            // Not enough balance
            if (cost > bankAccount.Balance)
            {
                ConsolePopup(args.Session, $"Insufficient funds (require {cost})");
                PlayDenySound(uid, component);
                return;
            }

            order.Approved = true;
            _idCardSystem.TryFindIdCard(player, out var idCard);
            order.Approver = idCard?.FullName ?? string.Empty;

            DeductFunds(bankAccount, cost);
            UpdateUIState(component, Get<StationSystem>().GetOwningStation(component.Owner));
        }

        private void OnRemoveOrderMessage(EntityUid uid, CargoConsoleComponent component, CargoConsoleRemoveOrderMessage args)
        {
            var orderDatabase = GetOrderDatabase(component);
            if (orderDatabase == null) return;
            RemoveOrder(orderDatabase, args.OrderNumber);
            UpdateUIState(component, Get<StationSystem>().GetOwningStation(component.Owner));
        }

        private void OnAddOrderMessage(EntityUid uid, CargoConsoleComponent component, CargoConsoleAddOrderMessage args)
        {
            if (args.Amount <= 0)
                return;

            var bank = GetBankAccount(component);
            if (bank == null) return;
            var orderDatabase = GetOrderDatabase(component);
            if (orderDatabase == null) return;

            var data = GetOrderData(args, GetNextIndex(orderDatabase));

            if (!TryAddOrder(orderDatabase, data))
            {
                PlayDenySound(uid, component);
                return;
            }

            UpdateUIState(component, Get<StationSystem>().GetOwningStation(component.Owner));
        }

        #endregion

        private void UpdateUIState(CargoConsoleComponent component, EntityUid? station)
        {
            if (station == null ||
                !_uiSystem.IsUiOpen(component.Owner, CargoConsoleUiKey.Key) ||
                !TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase) ||
                !TryComp<StationBankAccountComponent>(station, out var bankAccount)) return;

            var state = new CargoConsoleInterfaceState(
                MetaData(station.Value).EntityName,
                GetOrderCount(orderDatabase),
                orderDatabase.Capacity,
                bankAccount.Balance,
                orderDatabase.Orders.Values.ToList());

            _uiSystem.GetUiOrNull(component.Owner, CargoConsoleUiKey.Key)?.SetState(state);
        }

        private void ConsolePopup(ICommonSession session, string text)
        {
            Get<PopupSystem>().PopupCursor(text, Filter.SinglePlayer(session));
        }

        private void PlayDenySound(EntityUid uid, CargoConsoleComponent component)
        {
            SoundSystem.Play(component.ErrorSound.GetSound(), Filter.Pvs(uid, entityManager: EntityManager));
        }

        private CargoOrderData GetOrderData(CargoConsoleAddOrderMessage args, int index)
        {
            return new CargoOrderData(index, args.Requester, args.Reason, args.ProductId, args.Amount);
        }

        private int GetOrderCount(StationCargoOrderDatabaseComponent component)
        {
            var amount = 0;

            foreach (var (_, order) in component.Orders)
            {
                if (!order.Approved) continue;
                amount += order.Amount;
            }

            return amount;
        }

        public bool TryAddOrder(StationCargoOrderDatabaseComponent component, CargoOrderData data)
        {
            component.Orders.Add(data.OrderNumber, data);
            return true;
        }

        private int GetNextIndex(StationCargoOrderDatabaseComponent component)
        {
            var index = component.Index;
            component.Index++;
            return index;
        }

        public void RemoveOrder(StationCargoOrderDatabaseComponent component, int index)
        {
            if (!component.Orders.Remove(index)) return;
        }

        public void ClearOrders(StationCargoOrderDatabaseComponent component)
        {
            if (component.Orders.Count == 0) return;

            component.Orders.Clear();
            Dirty(component);
        }

        private void DeductFunds(StationBankAccountComponent component, int amount)
        {
            component.Balance = Math.Max(0, component.Balance - amount);
            Dirty(component);
        }

        #region Station

        private StationBankAccountComponent? GetBankAccount(CargoConsoleComponent component)
        {
            var station = Get<StationSystem>().GetOwningStation(component.Owner);

            TryComp<StationBankAccountComponent>(station, out var bankComponent);
            return bankComponent;
        }

        private StationCargoOrderDatabaseComponent? GetOrderDatabase(CargoConsoleComponent component)
        {
            var station = Get<StationSystem>().GetOwningStation(component.Owner);

            TryComp<StationCargoOrderDatabaseComponent>(station, out var orderComponent);
            return orderComponent;
        }

        #endregion
    }
}
