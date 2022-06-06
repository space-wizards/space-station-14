using Content.Server.Access.Systems;
using Content.Server.Cargo.Components;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.System;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

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

        [Dependency] private readonly IdCardSystem _idCardSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
        [Dependency] private readonly SignalLinkerSystem _linker = default!;

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
            }
        }

        private void OnShuttleMessage(EntityUid uid, CargoConsoleComponent component, CargoConsoleShuttleMessage args)
        {
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
        }

        private void OnApproveOrderMessage(EntityUid uid, CargoConsoleComponent component, CargoConsoleApproveOrderMessage args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} player)
                return;

            var orderDatabase = GetOrderDatabase(component);

            if (orderDatabase == null || !orderDatabase.Orders.TryGetValue(args.OrderNumber, out var order))
                return;

            if (_protoMan.TryIndex(order.ProductId, out CargoProductPrototype? product))
                return;

            var capacity = orderDatabase.Capacity;

            // Too much approved.
            if (order.Amount + orderDatabase.Orders.Count > orderDatabase.Capacity) return;

            // TODO: Check balance

            // TODO: Approve order

            // TODO: Change balance?

            if (
                (capacity.CurrentCapacity == capacity.MaxCapacity
                 || capacity.CurrentCapacity + order.Amount > capacity.MaxCapacity
                 || !_cargoConsoleSystem.CheckBalance(_bankAccount.Id, (-product.PointCost) * order.Amount)
                 || !_cargoConsoleSystem.ApproveOrder(uid, player, orders.Database.Id, msg.OrderNumber)
                 || !_cargoConsoleSystem.ChangeBalance(_bankAccount.Id, (-product.PointCost) * order.Amount))
            )
            {
                SoundSystem.Play(Filter.Pvs(uid), component.ErrorSound.GetSound(), uid, AudioParams.Default);
                return;
            }

            UpdateUIState();
        }

        private void OnRemoveOrderMessage(EntityUid uid, CargoConsoleComponent component, CargoConsoleRemoveOrderMessage args)
        {
            var orderDatabase = GetOrderDatabase(component);
            if (orderDatabase == null) return;
            RemoveOrder(orderDatabase, args.OrderNumber);
        }

        private void OnAddOrderMessage(EntityUid uid, CargoConsoleComponent component, CargoConsoleAddOrderMessage args)
        {
            if (args.Amount <= 0)
                return;

            var bank = GetBankAccount(component);
            if (bank == null) return;
            var orderDatabase = GetOrderDatabase(component);
            if (orderDatabase == null) return;

            var data = GetOrderData(args);

            if (!TryAddOrder(orderDatabase, data))
            {
                SoundSystem.Play(Filter.Pvs(uid, entityManager: EntityManager), component.ErrorSound.GetSound(), uid, AudioParams.Default);
            }
        }

        private CargoOrderData GetOrderData(CargoConsoleAddOrderMessage args)
        {
            return new CargoOrderData();
        }

        public bool TryAddOrder(StationCargoOrderDatabaseComponent component, CargoOrderData data)
        {
            var index = GetNextIndex(component);
            component.Orders.Add(index, data);
            Dirty(component);
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
            Dirty(component);
        }

        public void ClearOrders(StationCargoOrderDatabaseComponent component)
        {
            if (component.Orders.Count == 0) return;

            component.Orders.Clear();
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
