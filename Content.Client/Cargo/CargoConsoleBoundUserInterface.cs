using Content.Client.Cargo.Components;
using Content.Client.Cargo.UI;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using static Content.Shared.Cargo.Components.SharedCargoConsoleComponent;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Cargo
{
    public class CargoConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private CargoConsoleMenu? _menu;

        [ViewVariables]
        private CargoConsoleOrderMenu? _orderMenu;

        [ViewVariables]
        public GalacticMarketComponent? Market { get; private set; }

        [ViewVariables]
        public CargoOrderDatabaseComponent? Orders { get; private set; }

        [ViewVariables]
        public bool RequestOnly { get; private set; }

        [ViewVariables]
        public int BankId { get; private set; }

        [ViewVariables]
        public string? BankName { get; private set; }

        [ViewVariables]
        public int BankBalance { get; private set; }

        [ViewVariables]
        public (int CurrentCapacity, int MaxCapacity) ShuttleCapacity { get; private set; }

        private CargoProductPrototype? _product;

        public CargoConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent(Owner.Owner, out GalacticMarketComponent? market) ||
                !entMan.TryGetComponent(Owner.Owner, out CargoOrderDatabaseComponent? orders)) return;

            Market = market;
            Orders = orders;

            _menu = new CargoConsoleMenu(this);
            _orderMenu = new CargoConsoleOrderMenu();

            _menu.OnClose += Close;

            _menu.Populate();

            Market.OnDatabaseUpdated += _menu.PopulateProducts;
            Market.OnDatabaseUpdated += _menu.PopulateCategories;
            Orders.OnDatabaseUpdated += _menu.PopulateOrders;

            _menu.CallShuttleButton.OnPressed += (_) =>
            {
                SendMessage(new CargoConsoleShuttleMessage());
            };
            _menu.OnItemSelected += (args) =>
            {
                if (args.Button.Parent is not CargoProductRow row)
                    return;
                _product = row.Product;
                _orderMenu.Requester.Text = "";
                _orderMenu.Reason.Text = "";
                _orderMenu.Amount.Value = 1;
                _orderMenu.OpenCentered();
            };
            _menu.OnOrderApproved += ApproveOrder;
            _menu.OnOrderCanceled += RemoveOrder;
            _orderMenu.SubmitButton.OnPressed += (_) =>
            {
                if (AddOrder())
                {
                    _orderMenu.Close();
                }
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
                _menu?.UpdateRequestOnly();
            }
            BankId = cState.BankId;
            BankName = cState.BankName;
            BankBalance = cState.BankBalance;
            ShuttleCapacity = cState.ShuttleCapacity;
            _menu?.UpdateCargoCapacity();
            _menu?.UpdateBankData();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            if (Market != null && _menu != null)
            {
                Market.OnDatabaseUpdated -= _menu.PopulateProducts;
                Market.OnDatabaseUpdated -= _menu.PopulateCategories;
            }

            if (Orders != null && _menu != null)
            {
                Orders.OnDatabaseUpdated -= _menu.PopulateOrders;
            }

            _menu?.Dispose();
            _orderMenu?.Dispose();
        }

        private bool AddOrder()
        {
            int orderAmt = _orderMenu?.Amount.Value ?? 0;
            if (orderAmt < 1 || orderAmt > ShuttleCapacity.MaxCapacity)
            {
                return false;
            }

            SendMessage(new CargoConsoleAddOrderMessage(
                _orderMenu?.Requester.Text ?? "",
                _orderMenu?.Reason.Text ?? "",
                _product?.ID ?? "",
                orderAmt));

            return true;
        }

        private void RemoveOrder(ButtonEventArgs args)
        {
            if (args.Button.Parent?.Parent is not CargoOrderRow row || row.Order == null)
                return;

            SendMessage(new CargoConsoleRemoveOrderMessage(row.Order.OrderNumber));
        }

        private void ApproveOrder(ButtonEventArgs args)
        {
            if (args.Button.Parent?.Parent is not CargoOrderRow row || row.Order == null)
                return;

            if (ShuttleCapacity.CurrentCapacity == ShuttleCapacity.MaxCapacity)
                return;

            SendMessage(new CargoConsoleApproveOrderMessage(row.Order.OrderNumber));
            _menu?.UpdateCargoCapacity();
        }
    }
}
