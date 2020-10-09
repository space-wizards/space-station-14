using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components.Cargo;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Prototypes.Cargo;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Cargo
{
    public class CargoConsoleMenu : SS14Window
    {
        protected override Vector2? CustomSize => (400, 600);

        public CargoConsoleBoundUserInterface Owner { get; private set; }

        public event Action<BaseButton.ButtonEventArgs> OnItemSelected;
        public event Action<BaseButton.ButtonEventArgs> OnOrderApproved;
        public event Action<BaseButton.ButtonEventArgs> OnOrderCanceled;

        private List<string> _categoryStrings = new List<string>();

        private Label _accountNameLabel { get; set; }
        private Label _pointsLabel { get; set; }
        private Label _shuttleStatusLabel { get; set; }
        private Label _shuttleCapacityLabel { get; set; }
        private VBoxContainer _requests { get; set; }
        private VBoxContainer _orders { get; set; }
        private OptionButton _categories { get; set; }
        private LineEdit _searchBar { get; set; }

        public VBoxContainer Products { get; set; }
        public Button CallShuttleButton { get; set; }
        public Button PermissionsButton { get; set; }

        private string _category = null;

        public CargoConsoleMenu(CargoConsoleBoundUserInterface owner)
        {
            IoCManager.InjectDependencies(this);
            Owner = owner;

            if (Owner.RequestOnly)
                Title = Loc.GetString("Cargo Request Console");
            else
                Title = Loc.GetString("Cargo Shuttle Console");

            var rows = new VBoxContainer();

            var accountName = new HBoxContainer();
            var accountNameLabel = new Label {
                Text = Loc.GetString("Account Name: "),
                StyleClasses = { StyleNano.StyleClassLabelKeyText }
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
                Text = Loc.GetString("Points: "),
                StyleClasses = { StyleNano.StyleClassLabelKeyText }
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
                Text = Loc.GetString("Shuttle Status: "),
                StyleClasses = { StyleNano.StyleClassLabelKeyText }
            };
            _shuttleStatusLabel = new Label
            {
                Text = Loc.GetString("Away") // Shuttle.Status
            };
            shuttleStatus.AddChild(shuttleStatusLabel);
            shuttleStatus.AddChild(_shuttleStatusLabel);
            rows.AddChild(shuttleStatus);

            var shuttleCapacity = new HBoxContainer();
            var shuttleCapacityLabel = new Label
            {
                Text = Loc.GetString("Order Capacity: "),
                StyleClasses = { StyleNano.StyleClassLabelKeyText }
            };
            _shuttleCapacityLabel = new Label
            {
                Text = "0/20"
            };
            shuttleCapacity.AddChild(shuttleCapacityLabel);
            shuttleCapacity.AddChild(_shuttleCapacityLabel);
            rows.AddChild(shuttleCapacity);

            var buttons = new HBoxContainer();
            CallShuttleButton = new Button()
            {
                Text = Loc.GetString("Call Shuttle"),
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            PermissionsButton = new Button()
            {
                Text = Loc.GetString("Permissions"),
                TextAlign = Label.AlignMode.Center
            };
            buttons.AddChild(CallShuttleButton);
            buttons.AddChild(PermissionsButton);
            rows.AddChild(buttons);

            var category = new HBoxContainer();
            _categories = new OptionButton
            {
                Prefix = Loc.GetString("Categories: "),
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1
            };
            _searchBar = new LineEdit
            {
                PlaceHolder = Loc.GetString("Search"),
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1
            };
            category.AddChild(_categories);
            category.AddChild(_searchBar);
            rows.AddChild(category);

            var products = new ScrollContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 6
            };
            Products = new VBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            products.AddChild(Products);
            rows.AddChild(products);

            var requestsAndOrders = new PanelContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 6,
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Black }
            };
            var orderScrollBox = new ScrollContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            var rAndOVBox = new VBoxContainer();
            var requestsLabel = new Label { Text = Loc.GetString("Requests") };
            _requests = new VBoxContainer // replace with scroll box so that approval buttons can be added
            {
                StyleClasses = { "transparentItemList" },
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
            };
            var ordersLabel = new Label { Text = Loc.GetString("Orders") };
            _orders = new VBoxContainer
            {
                StyleClasses = { "transparentItemList" },
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
            };
            rAndOVBox.AddChild(requestsLabel);
            rAndOVBox.AddChild(_requests);
            rAndOVBox.AddChild(ordersLabel);
            rAndOVBox.AddChild(_orders);
            orderScrollBox.AddChild(rAndOVBox);
            requestsAndOrders.AddChild(orderScrollBox);
            rows.AddChild(requestsAndOrders);

            rows.AddChild(new TextureButton
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
            });
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
            SetCategoryText(args.Id);
            PopulateProducts();
        }

        private void OnSearchBarTextChanged(LineEdit.LineEditEventArgs args)
        {
            PopulateProducts();
        }

        private void SetCategoryText(int id)
        {
            if (id == 0)
                _category = null;
            else
                _category = _categoryStrings[id];
            _categories.SelectId(id);
        }

        /// <summary>
        ///     Populates the list of products that will actually be shown, using the current filters.
        /// </summary>
        public void PopulateProducts()
        {
            Products.RemoveAllChildren();

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
                    var button = new CargoProductRow();
                    button.Product = prototype;
                    button.ProductName.Text = prototype.Name;
                    button.PointCost.Text = prototype.PointCost.ToString();
                    button.Icon.Texture = prototype.Icon.Frame0();
                    button.MainButton.OnPressed += (args) =>
                    {
                        OnItemSelected?.Invoke(args);
                    };
                    Products.AddChild(button);
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

            _categoryStrings.Add(Loc.GetString("All"));

            var search = _searchBar.Text.Trim().ToLowerInvariant();
            foreach (var prototype in Owner.Market.Products)
            {
                if (!_categoryStrings.Contains(prototype.Category))
                {
                    _categoryStrings.Add(Loc.GetString(prototype.Category));
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
            _orders.RemoveAllChildren();
            _requests.RemoveAllChildren();
            foreach (var order in Owner.Orders.Orders)
            {
                var row = new CargoOrderRow();
                row.Order = order;
                row.Icon.Texture = Owner.Market.GetProduct(order.ProductId).Icon.Frame0();
                row.ProductName.Text = $"{Owner.Market.GetProduct(order.ProductId).Name} (x{order.Amount}) by {order.Requester}";
                row.Description.Text = $"Reasons: {order.Reason}";
                row.Cancel.OnPressed += (args) => { OnOrderCanceled?.Invoke(args); };
                if (order.Approved)
                {
                    row.Approve.Visible = false;
                    row.Cancel.Visible = false;
                    _orders.AddChild(row);
                }
                else
                {
                    if (Owner.RequestOnly)
                        row.Approve.Visible = false;
                    else
                        row.Approve.OnPressed += (args) => { OnOrderApproved?.Invoke(args); };
                    _requests.AddChild(row);
                }
            }
        }

        public void Populate()
        {
            PopulateProducts();
            PopulateCategories();
            PopulateOrders();
        }

        public void UpdateCargoCapacity()
        {
            _shuttleCapacityLabel.Text = $"{Owner.ShuttleCapacity.CurrentCapacity}/{Owner.ShuttleCapacity.MaxCapacity}";
        }

        public void UpdateBankData()
        {
            _accountNameLabel.Text = Owner.BankName;
            _pointsLabel.Text = Owner.BankBalance.ToString();
        }

        /// <summary>
        ///     Show/Hide Call Shuttle button and Approve buttons
        /// </summary>
        public void UpdateRequestOnly()
        {
            CallShuttleButton.Visible = !Owner.RequestOnly;
            foreach (CargoOrderRow row in _requests.Children)
            {
                row.Approve.Visible = !Owner.RequestOnly;
            }
        }
    }

    internal class CargoProductRow : PanelContainer
    {
        public CargoProductPrototype Product { get; set; }
        public TextureRect Icon { get; private set; }
        public Button MainButton { get; private set; }
        public Label ProductName { get; private set; }
        public Label PointCost { get; private set; }

        public CargoProductRow()
        {
            SizeFlagsHorizontal = SizeFlags.FillExpand;

            MainButton = new Button
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            AddChild(MainButton);

            var hBox = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };

            Icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(32.0f, 32.0f),
                RectClipContent = true
            };
            hBox.AddChild(Icon);

            ProductName = new Label
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            hBox.AddChild(ProductName);

            var panel = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat { BackgroundColor = new Color(37, 37, 42) },
            };
            PointCost = new Label
            {
                CustomMinimumSize = new Vector2(40.0f, 32.0f),
                Align = Label.AlignMode.Right
            };
            panel.AddChild(PointCost);
            hBox.AddChild(panel);

            AddChild(hBox);
        }
    }

    internal class CargoOrderRow : PanelContainer
    {
        public CargoOrderData Order { get; set; }
        public TextureRect Icon { get; private set; }
        public Label ProductName { get; private set; }
        public Label Description { get; private set; }
        public BaseButton Approve { get; private set; }
        public BaseButton Cancel { get; private set; }

        public CargoOrderRow()
        {
            SizeFlagsHorizontal = SizeFlags.FillExpand;

            var hBox = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            Icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(32.0f, 32.0f),
                RectClipContent = true
            };
            hBox.AddChild(Icon);

            var vBox = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            ProductName = new Label
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                StyleClasses = { StyleNano.StyleClassLabelSubText },
                ClipText = true
            };
            Description = new Label
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                StyleClasses = { StyleNano.StyleClassLabelSubText },
                ClipText = true
            };
            vBox.AddChild(ProductName);
            vBox.AddChild(Description);
            hBox.AddChild(vBox);

            Approve = new Button
            {
                Text = "Approve",
                StyleClasses = { StyleNano.StyleClassLabelSubText }
            };
            hBox.AddChild(Approve);

            Cancel = new Button
            {
                Text = "Cancel",
                StyleClasses = { StyleNano.StyleClassLabelSubText }
            };
            hBox.AddChild(Cancel);

            AddChild(hBox);
        }
    }
}
