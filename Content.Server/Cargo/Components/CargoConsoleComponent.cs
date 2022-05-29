using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Sound;
using Content.Server.MachineLinking.Components;
using Content.Shared.MachineLinking;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cargo.Components
{
    [RegisterComponent]
    public sealed class CargoConsoleComponent : SharedCargoConsoleComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private CargoBankAccount? _bankAccount;

        [ViewVariables]
        public CargoBankAccount? BankAccount
        {
            get => _bankAccount;
            private set
            {
                if (_bankAccount == value)
                {
                    return;
                }

                if (_bankAccount != null)
                {
                    _bankAccount.OnBalanceChange -= UpdateUIState;
                }

                _bankAccount = value;

                if (value != null)
                {
                    value.OnBalanceChange += UpdateUIState;
                }

                UpdateUIState();
            }
        }

        [DataField("requestOnly")]
        private bool _requestOnly = false;

        [DataField("errorSound")]
        private SoundSpecifier _errorSound = new SoundPathSpecifier("/Audio/Effects/error.ogg");

        private bool Powered => !_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;
        private CargoSystem _cargoConsoleSystem = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(CargoConsoleUiKey.Key);

        [DataField("senderPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string SenderPort = "OrderSender";

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn(out CargoOrderDatabaseComponent _);

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            _cargoConsoleSystem = EntitySystem.Get<CargoSystem>();
            BankAccount = _cargoConsoleSystem.StationAccount;
        }

        protected override void OnRemove()
        {
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= UserInterfaceOnOnReceiveMessage;
            }

            base.OnRemove();
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (!_entMan.TryGetComponent(Owner, out CargoOrderDatabaseComponent? orders))
            {
                return;
            }

            var message = serverMsg.Message;
            if (orders.Database == null)
                return;
            if (!Powered)
                return;
            switch (message)
            {
                case CargoConsoleAddOrderMessage msg:
                {
                    if (msg.Amount <= 0 || _bankAccount == null)
                    {
                        break;
                    }

                    if (!_cargoConsoleSystem.AddOrder(orders.Database.Id, msg.Requester, msg.Reason, msg.ProductId,
                        msg.Amount, _bankAccount.Id))
                    {
                        SoundSystem.Play(Filter.Pvs(Owner), _errorSound.GetSound(), Owner, AudioParams.Default);
                    }
                    break;
                }
                case CargoConsoleRemoveOrderMessage msg:
                {
                    _cargoConsoleSystem.RemoveOrder(orders.Database.Id, msg.OrderNumber);
                    break;
                }
                case CargoConsoleApproveOrderMessage msg:
                {
                    if (_requestOnly ||
                        !orders.Database.TryGetOrder(msg.OrderNumber, out var order) ||
                        _bankAccount == null)
                    {
                        break;
                    }

                    if (msg.Session.AttachedEntity is not {Valid: true} player)
                        break;

                    PrototypeManager.TryIndex(order.ProductId, out CargoProductPrototype? product);
                    if (product == null!)
                        break;
                    var capacity = _cargoConsoleSystem.GetCapacity(orders.Database.Id);
                    if (
                        (capacity.CurrentCapacity == capacity.MaxCapacity
                        || capacity.CurrentCapacity + order.Amount > capacity.MaxCapacity
                        || !_cargoConsoleSystem.CheckBalance(_bankAccount.Id, (-product.PointCost) * order.Amount)
                        || !_cargoConsoleSystem.ApproveOrder(Owner, player, orders.Database.Id, msg.OrderNumber)
                        || !_cargoConsoleSystem.ChangeBalance(_bankAccount.Id, (-product.PointCost) * order.Amount))
                        )
                    {
                        SoundSystem.Play(Filter.Pvs(Owner), _errorSound.GetSound(), Owner, AudioParams.Default);
                        break;
                    }

                    UpdateUIState();
                    break;
                }
                case CargoConsoleShuttleMessage _:
                {
                    // Jesus fucking christ Glass
                    //var approvedOrders = _cargoOrderDataManager.RemoveAndGetApprovedFrom(orders.Database);
                    //orders.Database.ClearOrderCapacity();

                    // TODO replace with shuttle code
                    EntityUid? cargoTelepad = null;

                    if (_entMan.TryGetComponent<SignalTransmitterComponent>(Owner, out var transmitter) &&
                        transmitter.Outputs.TryGetValue(SenderPort, out var telepad) &&
                        telepad.Count > 0)
                    {
                        // use most recent link
                        var pad = telepad[^1].Uid;
                        if (_entMan.HasComponent<CargoTelepadComponent>(pad) &&
                            _entMan.TryGetComponent<ApcPowerReceiverComponent?>(pad, out var powerReceiver) &&
                            powerReceiver.Powered)
                            cargoTelepad = pad;
                    }

                    if (cargoTelepad != null)
                    {
                        if (_entMan.TryGetComponent<CargoTelepadComponent?>(cargoTelepad.Value, out var telepadComponent))
                        {
                            var approvedOrders = _cargoConsoleSystem.RemoveAndGetApprovedOrders(orders.Database.Id);
                            orders.Database.ClearOrderCapacity();
                            foreach (var order in approvedOrders)
                            {
                                _cargoConsoleSystem.QueueTeleport(telepadComponent, order);
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void UpdateUIState()
        {
            if (_bankAccount == null || !_entMan.EntityExists(Owner))
            {
                return;
            }

            var id = _bankAccount.Id;
            var name = _bankAccount.Name;
            var balance = _bankAccount.Balance;
            var capacity = _cargoConsoleSystem.GetCapacity(id);
            UserInterface?.SetState(new CargoConsoleInterfaceState(_requestOnly, id, name, balance, capacity));
        }
    }
}
