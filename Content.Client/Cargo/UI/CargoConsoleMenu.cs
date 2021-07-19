using System;
using System.Collections.Generic;
using Content.Client.Stylesheets;
using Content.Shared.Cargo;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Cargo.UI
{
    public class CargoConsoleMenu : SS14Window
    {
        public CargoConsoleBoundUserInterface Owner { get; private set; }

        public event Action<ButtonEventArgs>? OnItemSelected;
        public event Action<ButtonEventArgs>? OnOrderApproved;
        public event Action<ButtonEventArgs>? OnOrderCanceled;

        private readonly List<string> _categoryStrings = new();

        private Label _accountNameLabel { get; set; }
        private Label _pointsLabel { get; set; }
        private Label _shuttleStatusLabel { get; set; }
        private Label _shuttleCapacityLabel { get; set; }
        private BoxContainer _requests { get; set; }
        private BoxContainer _orders { get; set; }
        private OptionButton _categories { get; set; }
        private LineEdit _searchBar { get; set; }

        public BoxContainer Products { get; set; }
        public Button CallShuttleButton { get; set; }
        public Button PermissionsButton { get; set; }

        private string? _category = null;

        public CargoConsoleMenu(CargoConsoleBoundUserInterface owner)
        {
            SetSize = MinSize = (400, 600);
            IoCManager.InjectDependencies(this);
            Owner = owner;

            if (Owner.RequestOnly)
                Title = Loc.GetString("cargo-console-menu-request-only-title");
            else
                Title = Loc.GetString("cargo-console-menu-title");

            var rows = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            var accountName = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            var accountNameLabel = new Label {
                Text = Loc.GetString("cargo-console-menu-account-name-label") + " ",
                StyleClasses = { StyleNano.StyleClassLabelKeyText }
            };
            _accountNameLabel = new Label {
                Text = Loc.GetString("cargo-console-menu-account-name-none-text") //Owner.Bank.Account.Name
            };
            accountName.AddChild(accountNameLabel);
            accountName.AddChild(_accountNameLabel);
            rows.AddChild(accountName);

            var points = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            var pointsLabel = new Label
            {
                Text = Loc.GetString("cargo-console-menu-points-label") + " ",
                StyleClasses = { StyleNano.StyleClassLabelKeyText }
            };
            _pointsLabel = new Label
            {
                Text = "0" //Owner.Bank.Account.Balance.ToString()
            };
            points.AddChild(pointsLabel);
            points.AddChild(_pointsLabel);
            rows.AddChild(points);

            var shuttleStatus = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            var shuttleStatusLabel = new Label
            {
                Text = Loc.GetString("cargo-console-menu-shuttle-status-label") + " ",
                StyleClasses = { StyleNano.StyleClassLabelKeyText }
            };
            _shuttleStatusLabel = new Label
            {
                Text = Loc.GetString("cargo-console-menu-shuttle-status-away-text") // Shuttle.Status
            };
            shuttleStatus.AddChild(shuttleStatusLabel);
            shuttleStatus.AddChild(_shuttleStatusLabel);
            rows.AddChild(shuttleStatus);

            var shuttleCapacity = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            var shuttleCapacityLabel = new Label
            {
                Text = Loc.GetString("cargo-console-menu-order-capacity-label") + " ",
                StyleClasses = { StyleNano.StyleClassLabelKeyText }
            };
            _shuttleCapacityLabel = new Label
            {
                Text = $"{0}/{20}"
            };
            shuttleCapacity.AddChild(shuttleCapacityLabel);
            shuttleCapacity.AddChild(_shuttleCapacityLabel);
            rows.AddChild(shuttleCapacity);

            var buttons = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            CallShuttleButton = new Button()
            {
                //Text = Loc.GetString("Call Shuttle"),
                Text = Loc.GetString("cargo-console-menu-call-shuttle-button"), //Shuttle code pending
                TextAlign = Label.AlignMode.Center,
                HorizontalExpand = true
            };
            PermissionsButton = new Button()
            {
                Text = Loc.GetString("cargo-console-menu-permissions-button"),
                TextAlign = Label.AlignMode.Center
            };
            buttons.AddChild(CallShuttleButton);
            buttons.AddChild(PermissionsButton);
            rows.AddChild(buttons);

            var category = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            _categories = new OptionButton
            {
                Prefix = Loc.GetString("cargo-console-menu-categories-label") + " ",
                HorizontalExpand = true,
                SizeFlagsStretchRatio = 1
            };
            _searchBar = new LineEdit
            {
                PlaceHolder = Loc.GetString("cargo-console-menu-search-bar-placeholder"),
                HorizontalExpand = true,
                SizeFlagsStretchRatio = 1
            };
            category.AddChild(_categories);
            category.AddChild(_searchBar);
            rows.AddChild(category);

            var products = new ScrollContainer()
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                SizeFlagsStretchRatio = 6
            };
            Products = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                HorizontalExpand = true,
                VerticalExpand = true
            };
            products.AddChild(Products);
            rows.AddChild(products);

            var requestsAndOrders = new PanelContainer
            {
                VerticalExpand = true,
                SizeFlagsStretchRatio = 6,
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Black }
            };
            var orderScrollBox = new ScrollContainer
            {
                VerticalExpand = true
            };
            var rAndOVBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            var requestsLabel = new Label { Text = Loc.GetString("cargo-console-menu-requests-label") };
            _requests = new BoxContainer // replace with scroll box so that approval buttons can be added
            {
                Orientation = LayoutOrientation.Vertical,
                StyleClasses = { "transparentItemList" },
                VerticalExpand = true,
                SizeFlagsStretchRatio = 1,
            };
            var ordersLabel = new Label { Text = Loc.GetString("cargo-console-menu-orders-label") };
            _orders = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                StyleClasses = { "transparentItemList" },
                VerticalExpand = true,
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
                VerticalExpand = true,
            });
            Contents.AddChild(rows);

            CallShuttleButton.OnPressed += OnCallShuttleButtonPressed;
            _searchBar.OnTextChanged += OnSearchBarTextChanged;
            _categories.OnItemSelected += OnCategoryItemSelected;
        }

        private void OnCallShuttleButtonPressed(ButtonEventArgs args)
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

            if (Owner.Market == null)
            {
                return;
            }

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

            if (Owner.Market == null)
            {
                return;
            }

            _categoryStrings.Add(Loc.GetString("cargo-console-menu-populate-categories-all-text"));

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

            if (Owner.Orders == null || Owner.Market == null)
            {
                return;
            }

            foreach (var order in Owner.Orders.Orders)
            {
                var row = new CargoOrderRow
                {
                    Order = order,
                    Icon = { Texture = Owner.Market.GetProduct(order.ProductId)?.Icon.Frame0() },
                    ProductName =
                    {
                        Text = Loc.GetString(
                            "cargo-console-menu-populate-orders-cargo-order-row-product-name-text",
                            ("productName", Owner.Market.GetProduct(order.ProductId)?.Name!),
                            ("orderAmount", order.Amount),
                            ("orderRequester", order.Requester))
                    },
                    Description = {Text = Loc.GetString("cargo-console-menu-order-reason-description",
                                                        ("reason", order.Reason))}
                };
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
        public CargoProductPrototype? Product { get; set; }
        public TextureRect Icon { get; private set; }
        public Button MainButton { get; private set; }
        public Label ProductName { get; private set; }
        public Label PointCost { get; private set; }

        public CargoProductRow()
        {
            HorizontalExpand = true;

            MainButton = new Button
            {
                HorizontalExpand = true,
                VerticalExpand = true
            };
            AddChild(MainButton);

            var hBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };

            Icon = new TextureRect
            {
                MinSize = new Vector2(32.0f, 32.0f),
                RectClipContent = true
            };
            hBox.AddChild(Icon);

            ProductName = new Label
            {
                HorizontalExpand = true
            };
            hBox.AddChild(ProductName);

            var panel = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat { BackgroundColor = new Color(37, 37, 42) },
            };
            PointCost = new Label
            {
                MinSize = new Vector2(40.0f, 32.0f),
                Align = Label.AlignMode.Right
            };
            panel.AddChild(PointCost);
            hBox.AddChild(panel);

            AddChild(hBox);
        }
    }

    internal class CargoOrderRow : PanelContainer
    {
        public CargoOrderData? Order { get; set; }
        public TextureRect Icon { get; private set; }
        public Label ProductName { get; private set; }
        public Label Description { get; private set; }
        public BaseButton Approve { get; private set; }
        public BaseButton Cancel { get; private set; }

        public CargoOrderRow()
        {
            HorizontalExpand = true;

            var hBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
            };

            Icon = new TextureRect
            {
                MinSize = new Vector2(32.0f, 32.0f),
                RectClipContent = true
            };
            hBox.AddChild(Icon);

            var vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                HorizontalExpand = true,
                VerticalExpand = true
            };
            ProductName = new Label
            {
                HorizontalExpand = true,
                StyleClasses = { StyleNano.StyleClassLabelSubText },
                ClipText = true
            };
            Description = new Label
            {
                HorizontalExpand = true,
                StyleClasses = { StyleNano.StyleClassLabelSubText },
                ClipText = true
            };
            vBox.AddChild(ProductName);
            vBox.AddChild(Description);
            hBox.AddChild(vBox);

            Approve = new Button
            {
                Text = Loc.GetString("cargo-console-menu-cargo-order-row-approve-button"),
                StyleClasses = { StyleNano.StyleClassLabelSubText }
            };
            hBox.AddChild(Approve);

            Cancel = new Button
            {
                Text = Loc.GetString("cargo-console-menu-cargo-order-row-cancel-button"),
                StyleClasses = { StyleNano.StyleClassLabelSubText }
            };
            hBox.AddChild(Cancel);

            AddChild(hBox);
        }
    }
}
