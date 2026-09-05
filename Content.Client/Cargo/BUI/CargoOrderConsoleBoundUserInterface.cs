using Content.Client.Cargo.UI;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.IdentityManagement;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Cargo.BUI
{
    public sealed partial class CargoOrderConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
    {
        [Dependency] private SharedCargoSystem _cargoSystem = default!;
        [Dependency] private IdentitySystem _identity = default!;

        [ViewVariables]
        private CargoConsoleMenu? _menu;

        /// <summary>
        /// This is the separate popup window for individual orders.
        /// </summary>
        [ViewVariables]
        private CargoConsoleOrderMenu? _orderMenu;

        [ViewVariables]
        public string? AccountName { get; private set; }

        [ViewVariables]
        public int BankBalance { get; private set; }

        [ViewVariables]
        public int OrderCapacity { get; private set; }

        [ViewVariables]
        public int OrderCount { get; private set; }

        /// <summary>
        /// Currently selected product
        /// </summary>
        [ViewVariables]
        private CargoProductPrototype? _product;

        protected override void Open()
        {
            base.Open();

            var spriteSystem = EntMan.System<SpriteSystem>();
            var dependencies = IoCManager.Instance!;
            _menu = new CargoConsoleMenu(Owner, EntMan, dependencies.Resolve<IPrototypeManager>(), spriteSystem);
            var localPlayer = dependencies.Resolve<IPlayerManager>().LocalEntity;
            var description = new FormattedMessage();

            var orderRequester = Loc.GetString("cargo-console-paper-approver-default");
            if (EntMan.EntityExists(localPlayer))
                orderRequester = _identity.GetIdentityShortInfo(localPlayer.Value, Owner) ?? orderRequester;

            _orderMenu = new CargoConsoleOrderMenu();

            _menu.OnClose += Close;

            _menu.OnItemSelected += (row) =>
            {
                if (row == null)
                    return;

                description.Clear();
                description.PushColor(Color.White); // Rich text default color is grey
                if (row.MainButton.ToolTip != null)
                    description.AddText(row.MainButton.ToolTip);

                _orderMenu.Description.SetMessage(description);
                _product = row.Product;
                _orderMenu.ProductName.Text = row.ProductName.Text;
                _orderMenu.PointCost.Text = row.PointCost.Text;
                _orderMenu.Requester.Text = orderRequester;
                _orderMenu.Reason.Text = "";
                _orderMenu.Amount.Value = 1;

                _orderMenu.OpenCentered();
                _orderMenu.SetPositionLast();
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

            _menu.OnAccountAction += (account, amount) =>
            {
                SendMessage(new CargoConsoleWithdrawFundsMessage(account, amount));
            };

            _menu.OnToggleUnboundedLimit += _ =>
            {
                SendMessage(new CargoConsoleToggleLimitMessage());
            };

            _menu.OpenCentered();
        }

        private void Populate(List<CargoOrderData> orders)
        {
            if (_menu == null)
                return;

            _menu.PopulateProducts();
            _menu.PopulateCategories();
            _menu.PopulateOrders(orders);
            _menu.PopulateAccountActions();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not CargoConsoleInterfaceState cState || !EntMan.TryGetComponent<CargoOrderConsoleComponent>(Owner, out var orderConsole))
                return;

            var station = EntMan.GetEntity(cState.Station);

            if (!_cargoSystem.TryGetAccountBalance(station, orderConsole.Account, out var balance))
                return;

            OrderCapacity = cState.Capacity;
            OrderCount = cState.Count;
            BankBalance = balance;

            AccountName = cState.Name;

            if (_menu == null)
                return;

            _menu.ProductCatalogue = cState.Products;
            _menu.ShuttleCapacityLabel.Text = Loc.GetString(
                "cargo-console-menu-order-capacity-number",
                ("count", OrderCount),
                ("capacity", OrderCapacity)
            );

            _menu?.UpdateStation(station);
            Populate(cState.Orders);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            _menu?.Dispose();
            _orderMenu?.Dispose();
        }

        private bool AddOrder()
        {
            var orderAmt = _orderMenu?.Amount.Value ?? 0;
            if (orderAmt < 1 || orderAmt > OrderCapacity)
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

        private void RemoveOrder(CargoOrderData? order)
        {
            if (order == null)
                return;

            SendMessage(new CargoConsoleRemoveOrderMessage(order.OrderId));
        }

        private void ApproveOrder(CargoOrderData? order)
        {
            if (order == null)
                return;

            if (OrderCount >= OrderCapacity)
                return;

            SendMessage(new CargoConsoleApproveOrderMessage(order.OrderId));
        }
    }
}
