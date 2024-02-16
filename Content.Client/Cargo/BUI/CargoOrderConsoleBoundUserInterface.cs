using Content.Shared.Cargo;
using Content.Client.Cargo.UI;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.IdentityManagement;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Cargo.BUI
{
    public sealed class CargoOrderConsoleBoundUserInterface : BoundUserInterface
    {
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

        public CargoOrderConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var spriteSystem = EntMan.System<SpriteSystem>();
            var dependencies = IoCManager.Instance!;
            _menu = new CargoConsoleMenu(Owner, EntMan, dependencies.Resolve<IPrototypeManager>(), spriteSystem);
            var localPlayer = dependencies.Resolve<IPlayerManager>().LocalEntity;
            var description = new FormattedMessage();

            string orderRequester;

            if (EntMan.TryGetComponent<MetaDataComponent>(localPlayer, out var metadata))
                orderRequester = Identity.Name(localPlayer.Value, EntMan);
            else
                orderRequester = string.Empty;

            _orderMenu = new CargoConsoleOrderMenu();

            _menu.OnClose += Close;

            _menu.OnItemSelected += (args) =>
            {
                if (args.Button.Parent is not CargoProductRow row)
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

        private void Populate(List<CargoOrderData> orders)
        {
            if (_menu == null) return;

            _menu.PopulateProducts();
            _menu.PopulateCategories();
            _menu.PopulateOrders(orders);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not CargoConsoleInterfaceState cState)
                return;

            OrderCapacity = cState.Capacity;
            OrderCount = cState.Count;
            BankBalance = cState.Balance;

            AccountName = cState.Name;

            Populate(cState.Orders);
            _menu?.UpdateCargoCapacity(OrderCount, OrderCapacity);
            _menu?.UpdateBankData(AccountName, BankBalance);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

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

        private void RemoveOrder(ButtonEventArgs args)
        {
            if (args.Button.Parent?.Parent is not CargoOrderRow row || row.Order == null)
                return;

            SendMessage(new CargoConsoleRemoveOrderMessage(row.Order.OrderId));
        }

        private void ApproveOrder(ButtonEventArgs args)
        {
            if (args.Button.Parent?.Parent is not CargoOrderRow row || row.Order == null)
                return;

            if (OrderCount >= OrderCapacity)
                return;

            SendMessage(new CargoConsoleApproveOrderMessage(row.Order.OrderId));
            // Most of the UI isn't predicted anyway so.
            // _menu?.UpdateCargoCapacity(OrderCount + row.Order.Amount, OrderCapacity);
        }
    }
}
