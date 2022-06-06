using Content.Server.Access.Systems;
using Content.Server.Cargo.Components;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.System;
using Content.Server.Power.Components;
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
            if (component._requestOnly ||
                !orders.Database.TryGetOrder(msg.OrderNumber, out var order) ||
                _bankAccount == null)
            {
                return;
            }

            if (msg.Session.AttachedEntity is not {Valid: true} player)
                return;

            _protoMan.TryIndex(order.ProductId, out CargoProductPrototype? product);
            if (product == null!)
                return;
            var capacity = _cargoConsoleSystem.GetCapacity(orders.Database.Id);
            if (
                (capacity.CurrentCapacity == capacity.MaxCapacity
                 || capacity.CurrentCapacity + order.Amount > capacity.MaxCapacity
                 || !_cargoConsoleSystem.CheckBalance(_bankAccount.Id, (-product.PointCost) * order.Amount)
                 || !_cargoConsoleSystem.ApproveOrder(uid, player, orders.Database.Id, msg.OrderNumber)
                 || !_cargoConsoleSystem.ChangeBalance(_bankAccount.Id, (-product.PointCost) * order.Amount))
            )
            {
                SoundSystem.Play(Filter.Pvs(uid), component._errorSound.GetSound(), uid, AudioParams.Default);
                return;
            }

            UpdateUIState();
        }

        private void OnRemoveOrderMessage(EntityUid uid, CargoConsoleComponent component, CargoConsoleRemoveOrderMessage args)
        {
            _cargoConsoleSystem.RemoveOrder(orders.Database.Id, msg.OrderNumber);
        }

        private void OnAddOrderMessage(EntityUid uid, CargoConsoleComponent component, CargoConsoleAddOrderMessage args)
        {
            if (msg.Amount <= 0 || _bankAccount == null)
            {
                return;
            }

            if (!_cargoConsoleSystem.AddOrder(orders.Database.Id, msg.Requester, msg.Reason, msg.ProductId,
                    msg.Amount, _bankAccount.Id))
            {
                SoundSystem.Play(Filter.Pvs(uid), _errorSound.GetSound(), uid, AudioParams.Default);
            }
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
    }
}
