#nullable enable
using Content.Server.Cargo;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
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

        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;
        private CargoConsoleSystem _cargoConsoleSystem = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(CargoConsoleUiKey.Key);

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
            if (!Owner.TryGetComponent(out CargoOrderDatabaseComponent? orders))
            {
                return;
            }

            var message = serverMsg.Message;
            if (!orders.ConnectedToDatabase)
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

                    _cargoOrderDataManager.AddOrder(orders.Database.Id, msg.Requester, msg.Reason, msg.ProductId, msg.Amount, _bankAccount.Id);
                    break;
                }
                case CargoConsoleRemoveOrderMessage msg:
                {
                    _cargoOrderDataManager.RemoveOrder(orders.Database.Id, msg.OrderNumber);
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

                    PrototypeManager.TryIndex(order.ProductId, out CargoProductPrototype product);
                    if (product == null!)
                        break;
                    var capacity = _cargoOrderDataManager.GetCapacity(orders.Database.Id);
                    if (capacity.CurrentCapacity == capacity.MaxCapacity)
                        break;
                    if (!_cargoConsoleSystem.ChangeBalance(_bankAccount.Id, (-product.PointCost) * order.Amount))
                        break;
                    _cargoOrderDataManager.ApproveOrder(orders.Database.Id, msg.OrderNumber);
                    UpdateUIState();
                    break;
                }
                case CargoConsoleShuttleMessage _:
                {
                    var approvedOrders = _cargoOrderDataManager.RemoveAndGetApprovedFrom(orders.Database);
                    orders.Database.ClearOrderCapacity();
                    // TODO replace with shuttle code

                    // TEMPORARY loop for spawning stuff on top of console
                    foreach (var order in approvedOrders)
                    {
                        if (!PrototypeManager.TryIndex(order.ProductId, out CargoProductPrototype product))
                            continue;
                        for (var i = 0; i < order.Amount; i++)
                        {
                            Owner.EntityManager.SpawnEntity(product.Product, Owner.Transform.Coordinates);
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
