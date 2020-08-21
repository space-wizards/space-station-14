#nullable enable
using Content.Server.Cargo;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Cargo;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Prototypes.Cargo;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Cargo
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class CargoConsoleComponent : SharedCargoConsoleComponent, IActivate
    {
        [Dependency] private readonly ICargoOrderDataManager _cargoOrderDataManager = default!;

        [ViewVariables]
        public int Points = 1000;

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

        private bool _requestOnly = false;

        private bool Powered => PowerReceiver == null || PowerReceiver.Powered;
        private CargoConsoleSystem _cargoConsoleSystem = default!;

        [ViewVariables]
        private BoundUserInterface? UserInterface =>
            Owner.TryGetComponent(out ServerUserInterfaceComponent? ui) &&
            ui.TryGetBoundUserInterface(CargoConsoleUiKey.Key, out var boundUi)
                ? boundUi
                : null;

        [ViewVariables]
        public GalacticMarketComponent? Market =>
            Owner.TryGetComponent(out GalacticMarketComponent? market) ? market : null;

        [ViewVariables]
        public CargoOrderDatabaseComponent? Orders =>
            Owner.TryGetComponent(out CargoOrderDatabaseComponent? orders) ? orders : null;

        private PowerReceiverComponent? PowerReceiver =>
            Owner.TryGetComponent(out PowerReceiverComponent? receiver) ? receiver : null;

        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.EnsureComponent(out GalacticMarketComponent _))
            {
                Logger.Warning($"Entity {Owner} at {Owner.Transform.MapPosition} had no {nameof(GalacticMarketComponent)}");
            }

            if (!Owner.EnsureComponent(out CargoOrderDatabaseComponent _))
            {
                Logger.Warning($"Entity {Owner} at {Owner.Transform.MapPosition} had no {nameof(GalacticMarketComponent)}");
            }

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            _cargoConsoleSystem = EntitySystem.Get<CargoConsoleSystem>();
            BankAccount = _cargoConsoleSystem.StationAccount;
        }

        public override void OnRemove()
        {
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            base.OnRemove();
        }

        /// <summary>
        ///    Reads data from YAML
        /// </summary>
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _requestOnly, "requestOnly", false);
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (Orders == null)
            {
                return;
            }

            var message = serverMsg.Message;
            if (!Orders.ConnectedToDatabase)
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

                    _cargoOrderDataManager.AddOrder(Orders.Database.Id, msg.Requester, msg.Reason, msg.ProductId, msg.Amount, _bankAccount.Id);
                    break;
                }
                case CargoConsoleRemoveOrderMessage msg:
                {
                    _cargoOrderDataManager.RemoveOrder(Orders.Database.Id, msg.OrderNumber);
                    break;
                }
                case CargoConsoleApproveOrderMessage msg:
                {
                    if (_requestOnly ||
                        !Orders.Database.TryGetOrder(msg.OrderNumber, out var order) ||
                        _bankAccount == null)
                    {
                        break;
                    }

                    _prototypeManager.TryIndex(order.ProductId, out CargoProductPrototype product);
                    if (product == null!)
                        break;
                    var capacity = _cargoOrderDataManager.GetCapacity(Orders.Database.Id);
                    if (capacity.CurrentCapacity == capacity.MaxCapacity)
                        break;
                    if (!_cargoConsoleSystem.ChangeBalance(_bankAccount.Id, (-product.PointCost) * order.Amount))
                        break;
                    _cargoOrderDataManager.ApproveOrder(Orders.Database.Id, msg.OrderNumber);
                    UpdateUIState();
                    break;
                }
                case CargoConsoleShuttleMessage _:
                {
                    var approvedOrders = _cargoOrderDataManager.RemoveAndGetApprovedFrom(Orders.Database);
                    Orders.Database.ClearOrderCapacity();
                    // TODO replace with shuttle code

                    // TEMPORARY loop for spawning stuff on top of console
                    foreach (var order in approvedOrders)
                    {
                        if (!_prototypeManager.TryIndex(order.ProductId, out CargoProductPrototype product))
                            continue;
                        for (var i = 0; i < order.Amount; i++)
                        {
                            Owner.EntityManager.SpawnEntity(product.Product, Owner.Transform.GridPosition);
                        }
                    }
                    break;
                }
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }
            if (!Powered)
                return;

            UserInterface?.Open(actor.playerSession);
        }

        private void UpdateUIState()
        {
            if (_bankAccount == null || !Owner.IsValid())
            {
                return;
            }

            var id = _bankAccount.Id;
            var name = _bankAccount.Name;
            var balance = _bankAccount.Balance;
            var capacity = _cargoOrderDataManager.GetCapacity(id);
            UserInterface?.SetState(new CargoConsoleInterfaceState(_requestOnly, id, name, balance, capacity));
        }
    }
}
