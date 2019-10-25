using Content.Server.Cargo;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Cargo;
using Content.Shared.Prototypes.Cargo;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Cargo
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class CargoConsoleComponent : SharedCargoConsoleComponent, IActivate
    {
#pragma warning disable 649
        [Dependency] private readonly IGalacticBankManager _galacticBankManager;
        [Dependency] private readonly ICargoOrderDataManager _cargoOrderDataManager;
#pragma warning restore 649

        [ViewVariables]
        public int Points = 1000;

        private BoundUserInterface _userInterface;

        [ViewVariables]
        public GalacticMarketComponent Market { get; private set; }
        [ViewVariables]
        public CargoOrderDatabaseComponent Orders { get; private set; }
        [ViewVariables]
        public int BankId { get; private set; }

        private bool _requestOnly = false;

        public override void Initialize()
        {
            base.Initialize();
            Market = Owner.GetComponent<GalacticMarketComponent>();
            Orders = Owner.GetComponent<CargoOrderDatabaseComponent>();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(CargoConsoleUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            _galacticBankManager.AddComponent(this);
            BankId = 0;
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            var message = serverMsg.Message;
            switch (message)
            {
                case CargoConsoleAddOrderMessage msg:
                {
                    _prototypeManager.TryIndex(msg.ProductId, out CargoProductPrototype product);
                    if (product == null)
                        break;
                    if (!_requestOnly && !_galacticBankManager.ChangeBalance(BankId, -product.PointCost))
                        break;
                    _cargoOrderDataManager.AddOrder(Orders.Database.Id, msg.Requester, msg.Reason, msg.ProductId, msg.Amount, BankId, !_requestOnly);
                    break;
                }
                case CargoConsoleShuttleMessage _:
                {
                    if (!Orders.ConnectedToDatabase)
                        break;
                    var approvedOrders = _cargoOrderDataManager.RemoveAndGetApprovedFrom(Orders.Database);
                    foreach (var order in approvedOrders)
                    {
                        if (!_prototypeManager.TryIndex(order.ProductId, out CargoProductPrototype product))
                            continue;
                        Owner.EntityManager.SpawnEntityAt(product.Product, Owner.Transform.GridPosition);
                    }
                    break;
                }
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            _userInterface.Open(actor.playerSession);
        }

        public void SetState(int id, string name, int balance)
        {
            _userInterface.SetState(new CargoConsoleInterfaceState(id, name, balance));
        }
    }
}
