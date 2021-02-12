using Content.Client.UserInterface.Cargo;
using Content.Shared.GameObjects.Components.Cargo;
using Content.Shared.Prototypes.Cargo;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Cargo
{
    public class CargoConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private CargoConsoleMenu _menu;
        [ViewVariables]
        private CargoConsoleOrderMenu _orderMenu;

        [ViewVariables]
        public GalacticMarketComponent Market { get; private set; }
        [ViewVariables]
        public CargoOrderDatabaseComponent Orders { get; private set; }
        [ViewVariables]
        public bool RequestOnly { get; private set; }
        [ViewVariables]
        public int BankId { get; private set; }
        [ViewVariables]
        public string BankName { get; private set; }
        [ViewVariables]
        public int BankBalance { get; private set; }
        [ViewVariables]
        public (int CurrentCapacity, int MaxCapacity) ShuttleCapacity { get; private set; }

        private CargoProductPrototype _product;

        public CargoConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            if (!Owner.Owner.TryGetComponent(out GalacticMarketComponent market)
            ||  !Owner.Owner.TryGetComponent(out CargoOrderDatabaseComponent orders)) return;

            Market = market;
            Orders = orders;

            _menu = new CargoConsoleMenu(this);
            _orderMenu = new CargoConsoleOrderMenu();

            _menu.OnClose += Close;

            _menu.Populate();

            Market.OnDatabaseUpdated += _menu.PopulateProducts;
            Market.OnDatabaseUpdated += _menu.PopulateCategories;
            Orders.OnDatabaseUpdated += _menu.PopulateOrders;

            _menu.CallShuttleButton.OnPressed += (args) =>
            {
                SendMessage(new SharedCargoConsoleComponent.CargoConsoleShuttleMessage());
            };
            _menu.OnItemSelected += (args) =>
            {
                if (args.Button.Parent is not CargoProductRow row)
                    return;
                _product = row.Product;
                _orderMenu.Requester.Text = null;
                _orderMenu.Reason.Text = null;
                _orderMenu.Amount.Value = 1;
                _orderMenu.OpenCentered();
            };
            _menu.OnOrderApproved += ApproveOrder;
            _menu.OnOrderCanceled += RemoveOrder;
            _orderMenu.SubmitButton.OnPressed += (args) =>
            {
                AddOrder();
                _orderMenu.Close();
            };

            _menu.OpenCentered();

        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not CargoConsoleInterfaceState cState)
                return;
            if (RequestOnly != cState.RequestOnly)
            {
                RequestOnly = cState.RequestOnly;
                _menu.UpdateRequestOnly();
            }
            BankId = cState.BankId;
            BankName = cState.BankName;
            BankBalance = cState.BankBalance;
            ShuttleCapacity = cState.ShuttleCapacity;
            _menu.UpdateCargoCapacity();
            _menu.UpdateBankData();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            Market.OnDatabaseUpdated -= _menu.PopulateProducts;
            Market.OnDatabaseUpdated -= _menu.PopulateCategories;
            Orders.OnDatabaseUpdated -= _menu.PopulateOrders;
            _menu?.Dispose();
            _orderMenu?.Dispose();
        }

        internal void AddOrder()
        {
            SendMessage(new SharedCargoConsoleComponent.CargoConsoleAddOrderMessage(_orderMenu.Requester.Text,
                _orderMenu.Reason.Text, _product.ID, _orderMenu.Amount.Value));
        }

        internal void RemoveOrder(BaseButton.ButtonEventArgs args)
        {
            if (args.Button.Parent.Parent is not CargoOrderRow row)
                return;
            SendMessage(new SharedCargoConsoleComponent.CargoConsoleRemoveOrderMessage(row.Order.OrderNumber));
        }

        internal void ApproveOrder(BaseButton.ButtonEventArgs args)
        {
            if (args.Button.Parent.Parent is not CargoOrderRow row)
                return;
            if (ShuttleCapacity.CurrentCapacity == ShuttleCapacity.MaxCapacity)
                return;
            SendMessage(new SharedCargoConsoleComponent.CargoConsoleApproveOrderMessage(row.Order.OrderNumber));
            _menu?.UpdateCargoCapacity();
        }
    }
}
