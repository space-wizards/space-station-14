using Content.Client.UserInterface.Cargo;
using Content.Shared.GameObjects.Components.Cargo;
using Content.Shared.Prototypes.Cargo;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
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
        public int BankId { get; private set; }
        public string BankName { get; private set; }
        public int BankBalance { get; private set; }

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
            _menu.Products.OnItemSelected += (args) =>
            {
                _product = _menu.ProductPrototypes[args.ItemIndex];
                _orderMenu.OpenCenteredMinSize();
            };
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

            if (!(state is CargoConsoleInterfaceState cstate))
                return;
            BankId = cstate.Id;
            BankName = cstate.Name;
            BankBalance = cstate.Balance;
            _menu.UpdateBankData();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _menu?.Dispose();
            _orderMenu?.Dispose();
        }

        internal void AddOrder()
        {
            SendMessage(new SharedCargoConsoleComponent.CargoConsoleAddOrderMessage(_orderMenu.Requester.Text,
                _orderMenu.Reason.Text, _product.ID, _orderMenu.Amount.Value));
        }
    }
}
