using Content.Client.GameObjects.Components.Cargo;
using Content.Shared.Prototypes.Cargo;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using System.Collections.Generic;

namespace Content.Client.UserInterface.Cargo
{
    public class CargoConsoleMenu : SS14Window
    {
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _loc;
#pragma warning restore 649

        protected override Vector2? CustomSize => (400, 600);

        public CargoConsoleBoundUserInterface Owner { get; private set; }

        public List<CargoProductPrototype> ProductPrototypes = new List<CargoProductPrototype>();
        public List<CargoOrderData> RequestData = new List<CargoOrderData>();

        private List<CargoOrderData> _orderData = new List<CargoOrderData>();
        private List<string> _categoryStrings = new List<string>();

        private Label _accountNameLabel { get; set; }
        private Label _pointsLabel { get; set; }
        private Label _shuttleStatusLabel { get; set; }
        private ItemList _requests { get; set; }
        private ItemList _orders { get; set; }
        private OptionButton _categories { get; set; }
        private LineEdit _searchBar { get; set; }

        public ItemList Products { get; set; }
        public Button CallShuttleButton { get; set; }
        public Button PermissionsButton { get; set; }

        private string _category = null;

        public CargoConsoleMenu(CargoConsoleBoundUserInterface owner)
        {
            IoCManager.InjectDependencies(this);
            Owner = owner;

            Title = _loc.GetString("Cargo Console");

            var rows = new VBoxContainer
            {
                MarginTop = 0
            };

            var accountName = new HBoxContainer()
            {
                MarginTop = 0
            };
            var accountNameLabel = new Label {
                Text = _loc.GetString("Account Name: "),
                StyleClasses = { NanoStyle.StyleClassLabelKeyText }
            };
            _accountNameLabel = new Label {
                Text = "None" //Owner.Bank.Account.Name
            };
            accountName.AddChild(accountNameLabel);
            accountName.AddChild(_accountNameLabel);
            rows.AddChild(accountName);

            var points = new HBoxContainer();
            var pointsLabel = new Label
            {
                Text = _loc.GetString("Points: "),
                StyleClasses = { NanoStyle.StyleClassLabelKeyText }
            };
            _pointsLabel = new Label
            {
                Text = "0" //Owner.Bank.Account.Balance.ToString()
            };
            points.AddChild(pointsLabel);
            points.AddChild(_pointsLabel);
            rows.AddChild(points);

            var shuttleStatus = new HBoxContainer();
            var shuttleStatusLabel = new Label
            {
                Text = _loc.GetString("Shuttle Status: "),
                StyleClasses = { NanoStyle.StyleClassLabelKeyText }
            };
            _shuttleStatusLabel = new Label
            {
                Text = _loc.GetString("Away") // Shuttle.Status
            };
            shuttleStatus.AddChild(shuttleStatusLabel);
            shuttleStatus.AddChild(_shuttleStatusLabel);
            rows.AddChild(shuttleStatus);

            var buttons = new HBoxContainer();
            CallShuttleButton = new Button()
            {
                Text = _loc.GetString("Call Shuttle"),
                TextAlign = Button.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            PermissionsButton = new Button()
            {
                Text = _loc.GetString("Permissions"),
                TextAlign = Button.AlignMode.Center
            };
            buttons.AddChild(CallShuttleButton);
            buttons.AddChild(PermissionsButton);
            rows.AddChild(buttons);

            var category = new HBoxContainer();
            _categories = new OptionButton
            {
                Text = "Categories:",
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1
            };
            _searchBar = new LineEdit
            {
                PlaceHolder = _loc.GetString("Search"),
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1
            };
            category.AddChild(_categories);
            category.AddChild(_searchBar);
            rows.AddChild(category);

            // replace with scroll box so that it can be [[Icon][Name]][#][Cost]
            // if icon/name is clicked just ask for reason
            // if # is clicked ask for reason and number of items to get
            Products = new ItemList()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 6
            };
            rows.AddChild(Products);

            var requestsAndOrders = new PanelContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 6,
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Black }
            };
            var rAndOVBox = new VBoxContainer();
            var requestsLabel = new Label { Text = _loc.GetString("Requests") };
            _requests = new ItemList // replace with scroll box so that approval buttons can be added
            {
                StyleClasses = { "transparentItemList" },
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
            };
            var ordersLabel = new Label { Text = _loc.GetString("Orders") };
            _orders = new ItemList
            {
                StyleClasses = { "transparentItemList" },
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
            };
            rAndOVBox.AddChild(requestsLabel);
            rAndOVBox.AddChild(_requests);
            rAndOVBox.AddChild(ordersLabel);
            rAndOVBox.AddChild(_orders);
            requestsAndOrders.AddChild(rAndOVBox);
            rows.AddChild(requestsAndOrders);

            Contents.AddChild(rows);

            CallShuttleButton.OnPressed += OnCallShuttleButtonPressed;
            _searchBar.OnTextChanged += OnSearchBarTextChanged;
            _categories.OnItemSelected += OnCategoryItemSelected;
        }

        private void OnCallShuttleButtonPressed(BaseButton.ButtonEventArgs args)
        {
        }

        private void OnCategoryItemSelected(OptionButton.ItemSelectedEventArgs args)
        {
            if (args.Id == 0)
            {
                _category = null;
            }
            else
            {
                _category = _categoryStrings[args.Id];
            }
            _categories.SelectId(args.Id);
            //_categories.Text = _categories.GetItem
            PopulateProducts();

        }

        private void OnSearchBarTextChanged(LineEdit.LineEditEventArgs args)
        {
            PopulateProducts();
        }

        /// <summary>
        ///     Populates the list of products that will actually be shown, using the current filters.
        /// </summary>
        public void PopulateProducts()
        {
            ProductPrototypes.Clear();
            Products.Clear();

            var search = _searchBar.Text.Trim().ToLowerInvariant();
            foreach (var prototype in Owner.Market.Products)
            {
                // if no search or category
                // else if search
                // else if category and not search
                if ((search.Length == 0 && _category == null) ||
                    (search.Length != 0 && prototype.Name.ToLowerInvariant().Contains(search)) ||
                    (search.Length == 0 && _category != null && prototype.Category.Equals(_category)))
                {
                    ProductPrototypes.Add(prototype);
                    Products.AddItem(prototype.Name + " (" + prototype.PointCost + ")", prototype.Icon.Frame0());
                }
            }
        }

        /// <summary>
        ///     Populates the list of products that will actually be shown, using the current filters.
        /// </summary>
        public void PopulateCategories()
        {
            _categoryStrings.Clear();
            _categories.Clear();

            _categoryStrings.Add("All");

            var search = _searchBar.Text.Trim().ToLowerInvariant();
            foreach (var prototype in Owner.Market.Products)
            {
                if (!_categoryStrings.Contains(prototype.Category))
                {
                    _categoryStrings.Add(prototype.Category);
                }
            }
            _categoryStrings.Sort();
            foreach (var str in _categoryStrings)
            {
                _categories.AddItem(str);
            }
        }

        /// <summary>
        ///     Populates the list of orders and requests.
        /// </summary>
        public void PopulateOrders()
        {
            RequestData.Clear();
            _orderData.Clear();

            _requests.Clear();
            _orders.Clear();

            foreach (var order in Owner.Orders.Orders)
            {
                var str = $"{Owner.Market.GetProduct(order.ProductId).Name} (x{order.Amount}) by {order.Requester} reason: {order.Reason}";
                if (order.Approved)
                {
                    _orderData.Add(order);
                    _orders.AddItem(str, Owner.Market.GetProduct(order.ProductId).Icon.Frame0());
                }
                else
                {
                    RequestData.Add(order);
                    _requests.AddItem(str, Owner.Market.GetProduct(order.ProductId).Icon.Frame0());
                }
            }
        }

        public void Populate()
        {
            PopulateProducts();
            PopulateCategories();
            PopulateOrders();
        }

        public void UpdateBankData()
        {
            _accountNameLabel.Text = Owner.BankName;
            _pointsLabel.Text = Owner.BankBalance.ToString();
        }
    }
}
