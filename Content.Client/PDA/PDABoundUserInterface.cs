using System;
using Content.Client.Examine;
using Content.Client.Message;
using Content.Shared.PDA;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.PDA
{
    [UsedImplicitly]
    public class PDABoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        private PDAMenu? _menu;
        private PDAMenuPopup? _failPopup;

        public PDABoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            SendMessage(new PDARequestUpdateInterfaceMessage());
            _menu = new PDAMenu(this, _prototypeManager);
            _menu.OpenToLeft();
            _menu.OnClose += Close;
            _menu.FlashLightToggleButton.OnToggled += _ =>
            {
                SendMessage(new PDAToggleFlashlightMessage());
            };

            _menu.EjectIDButton.OnPressed += _ =>
            {
                SendMessage(new PDAEjectIDMessage());
            };

            _menu.EjectPenButton.OnPressed += _ =>
            {
                SendMessage(new PDAEjectPenMessage());
            };

            _menu.MasterTabContainer.OnTabChanged += i =>
            {
                var tab = _menu.MasterTabContainer.GetChild(i);
                if (tab == _menu.UplinkTabContainer)
                {
                    SendMessage(new PDARequestUpdateInterfaceMessage());
                }
            };

            _menu.OnListingButtonPressed += (_, listing) =>
            {
                if (_menu.CurrentLoggedInAccount?.DataBalance < listing.Price)
                {
                    _failPopup = new PDAMenuPopup(Loc.GetString("pda-bound-user-interface-insufficient-funds-popup"));
                    _userInterfaceManager.ModalRoot.AddChild(_failPopup);
                    _failPopup.Open(UIBox2.FromDimensions(_menu.Position.X + 150, _menu.Position.Y + 60, 156, 24));
                    _menu.OnClose += () =>
                    {
                        _failPopup.Dispose();
                    };
                }

                SendMessage(new PDAUplinkBuyListingMessage(listing.ItemId));
            };

            _menu.OnCategoryButtonPressed += (_, category) =>
            {
                _menu.CurrentFilterCategory = category;
                SendMessage(new PDARequestUpdateInterfaceMessage());

            };
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_menu == null)
            {
                return;
            }

            switch (state)
            {
                case PDAUpdateState msg:
                {
                    _menu.FlashLightToggleButton.Pressed = msg.FlashlightEnabled;

                    if (msg.PDAOwnerInfo.ActualOwnerName != null)
                    {
                        _menu.PDAOwnerLabel.SetMarkup(Loc.GetString("comp-pda-ui-owner",
                            ("ActualOwnerName", msg.PDAOwnerInfo.ActualOwnerName)));
                    }


                    if (msg.PDAOwnerInfo.IdOwner != null || msg.PDAOwnerInfo.JobTitle != null)
                    {
                        _menu.IDInfoLabel.SetMarkup(Loc.GetString("comp-pda-ui",
                            ("Owner",msg.PDAOwnerInfo.IdOwner ?? "Unknown"),
                            ("JobTitle",msg.PDAOwnerInfo.JobTitle ?? "Unassigned")));
                    }
                    else
                    {
                        _menu.IDInfoLabel.SetMarkup(Loc.GetString("comp-pda-ui-blank"));
                    }

                    _menu.EjectIDButton.Visible = msg.PDAOwnerInfo.IdOwner != null || msg.PDAOwnerInfo.JobTitle != null;
                    _menu.EjectPenButton.Visible = msg.HasPen;

                    if (msg.Account != null)
                    {
                        _menu.CurrentLoggedInAccount = msg.Account;
                        var balance = msg.Account.DataBalance;
                        var weightedColor = GetWeightedColorString(balance);
                        _menu.BalanceInfo.SetMarkup(Loc.GetString("pda-bound-user-interface-tc-balance-popup",
                                                                 ("weightedColor",weightedColor),
                                                                 ("balance",balance)));
                    }

                    if (msg.Listings != null)
                    {
                        _menu.ClearListings();
                        foreach (var item in msg.Listings) //Should probably chunk these out instead. to-do if this clogs the internet tubes.
                        {
                            _menu.AddListingGui(item);
                        }
                    }

                    _menu.MasterTabContainer.SetTabVisible(1, msg.Account != null);
                    break;
                }
            }
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }

        /// <summary>
        /// This is shitcode. It is, however, "PJB-approved shitcode".
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Color GetWeightedColor(int x)
        {
            var weightedColor = Color.Gray;
            if (x <= 0)
            {
                return weightedColor;
            }
            if (x <= 5)
            {
                weightedColor = Color.Green;
            }
            else if (x > 5 && x < 10)
            {
                weightedColor = Color.Yellow;
            }
            else if (x > 10 && x <= 20)
            {
                weightedColor = Color.Orange;
            }
            else if (x > 20 && x <= 50)
            {
                weightedColor = Color.Purple;
            }

            return weightedColor;
        }

        public static string GetWeightedColorString(int x)
        {
            var weightedColor = "gray";
            if (x <= 0)
            {
                return weightedColor;
            }

            if (x <= 5)
            {
                weightedColor = "green";
            }
            else if (x > 5 && x < 10)
            {
                weightedColor = "yellow";
            }
            else if (x > 10 && x <= 20)
            {
                weightedColor = "yellow";
            }
            else if (x > 20 && x <= 50)
            {
                weightedColor = "purple";
            }
            return weightedColor;
        }

        public sealed class PDAMenuPopup : Popup
        {
            public PDAMenuPopup(string text)
            {
                var label = new RichTextLabel();
                label.SetMessage(text);
                AddChild(new PanelContainer
                {
                    StyleClasses = { ExamineSystem.StyleClassEntityTooltip },
                    Children = { label }
                });
            }
        }

        private class PDAMenu : SS14Window
        {
            private PDABoundUserInterface _owner { get; }

            public Button FlashLightToggleButton { get; }
            public Button EjectIDButton { get; }
            public Button EjectPenButton { get; }

            public readonly TabContainer MasterTabContainer;

            public RichTextLabel PDAOwnerLabel { get; }
            public PanelContainer IDInfoContainer { get; }
            public RichTextLabel IDInfoLabel { get; }

            public VBoxContainer UplinkTabContainer { get; }

            protected readonly HSplitContainer CategoryAndListingsContainer;

            private readonly IPrototypeManager _prototypeManager;

            public readonly VBoxContainer UplinkListingsContainer;

            public readonly VBoxContainer CategoryListContainer;
            public readonly RichTextLabel BalanceInfo;
            public event Action<ButtonEventArgs, UplinkListingData>? OnListingButtonPressed;
            public event Action<ButtonEventArgs, UplinkCategory>? OnCategoryButtonPressed;

            public UplinkCategory CurrentFilterCategory
            {
                get => _currentFilter;
                set
                {
                    if (value.GetType() != typeof(UplinkCategory))
                    {
                        return;
                    }

                    _currentFilter = value;
                }
            }

            public UplinkAccountData? CurrentLoggedInAccount
            {
                get => _loggedInUplinkAccount;
                set => _loggedInUplinkAccount = value;
            }

            private UplinkCategory _currentFilter;
            private UplinkAccountData? _loggedInUplinkAccount;

            public PDAMenu(PDABoundUserInterface owner, IPrototypeManager prototypeManager)
            {
                MinSize = SetSize = (512, 256);

                _owner = owner;
                _prototypeManager = prototypeManager;
                Title = Loc.GetString("comp-pda-ui-menu-title");

                #region MAIN_MENU_TAB
                //Main menu
                PDAOwnerLabel = new RichTextLabel
                {
                };

                IDInfoLabel = new RichTextLabel()
                {
                    HorizontalExpand = true,
                };

                EjectIDButton = new Button
                {
                    Text = Loc.GetString("comp-pda-ui-eject-id-button"),
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center
                };
                EjectPenButton = new Button
                {
                    Text = Loc.GetString("comp-pda-ui-eject-pen-button"),
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center
                };

                var innerHBoxContainer = new HBoxContainer
                {
                    Children =
                    {
                        IDInfoLabel,
                        EjectIDButton,
                        EjectPenButton
                    }
                };

                IDInfoContainer = new PanelContainer
                {
                    Children =
                    {
                        innerHBoxContainer,
                    }
                };

                FlashLightToggleButton = new Button
                {
                    Text = Loc.GetString("comp-pda-ui-toggle-flashlight-button"),
                    ToggleMode = true,
                };

                var mainMenuTabContainer = new VBoxContainer
                {
                    VerticalExpand = true,
                    HorizontalExpand = true,
                    MinSize = (50, 50),

                    Children =
                    {
                        PDAOwnerLabel,
                        IDInfoContainer,
                        FlashLightToggleButton
                    }
                };

                #endregion

                #region UPLINK_TAB
                //Uplink Tab
                CategoryListContainer = new VBoxContainer
                {
                };

                BalanceInfo = new RichTextLabel
                {
                    HorizontalAlignment = HAlignment.Center,
                };

                //Red background container.
                var masterPanelContainer = new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Black },
                    VerticalExpand = true
                };

                //This contains both the panel of the category buttons and the listings box.
                CategoryAndListingsContainer = new HSplitContainer
                {
                    VerticalExpand = true,
                };


                var uplinkShopScrollContainer = new ScrollContainer
                {
                    HorizontalExpand = true,
                    VerticalExpand = true,
                    SizeFlagsStretchRatio = 2,
                    MinSize = (100, 256)
                };

                //Add the category list to the left side. The store items to center.
                var categoryListContainerBackground = new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Gray.WithAlpha(0.02f) },
                    VerticalExpand = true,
                    Children =
                    {
                        CategoryListContainer
                    }
                };

                CategoryAndListingsContainer.AddChild(categoryListContainerBackground);
                CategoryAndListingsContainer.AddChild(uplinkShopScrollContainer);
                masterPanelContainer.AddChild(CategoryAndListingsContainer);

                //Actual list of buttons for buying a listing from the uplink.
                UplinkListingsContainer = new VBoxContainer
                {
                    HorizontalExpand = true,
                    VerticalExpand = true,
                    SizeFlagsStretchRatio = 2,
                    MinSize = (100, 256),
                };
                uplinkShopScrollContainer.AddChild(UplinkListingsContainer);

                var innerVboxContainer = new VBoxContainer
                {
                    VerticalExpand = true,

                    Children =
                    {
                        BalanceInfo,
                        masterPanelContainer
                    }
                };

                UplinkTabContainer = new VBoxContainer
                {
                    Children =
                    {
                        innerVboxContainer
                    }
                };
                PopulateUplinkCategoryButtons();
                #endregion

                //The master menu that contains all of the tabs.
                MasterTabContainer = new TabContainer
                {
                    Children =
                    {
                        mainMenuTabContainer,
                    }
                };

                //Add all the tabs to the Master container.
                MasterTabContainer.SetTabTitle(0, Loc.GetString("pda-bound-user-interface-main-menu-tab-title"));
                MasterTabContainer.AddChild(UplinkTabContainer);
                MasterTabContainer.SetTabTitle(1, Loc.GetString("pda-bound-user-interface-uplink-tab-title"));
                Contents.AddChild(MasterTabContainer);
            }

            private void PopulateUplinkCategoryButtons()
            {

                foreach (UplinkCategory cat in Enum.GetValues(typeof(UplinkCategory)))
                {

                    var catButton = new PDAUplinkCategoryButton
                    {
                        Text = Loc.GetString(cat.ToString()),
                        ButtonCategory = cat

                    };
                    //It'd be neat if it could play a cool tech ping sound when you switch categories,
                    //but right now there doesn't seem to be an easy way to do client-side audio without still having to round trip to the server and
                    //send to a specific client INetChannel.
                    catButton.OnPressed += args => OnCategoryButtonPressed?.Invoke(args, catButton.ButtonCategory);

                    CategoryListContainer.AddChild(catButton);
                }

            }

            public void AddListingGui(UplinkListingData listing)
            {
                if (!_prototypeManager.TryIndex(listing.ItemId, out EntityPrototype? prototype) || listing.Category != CurrentFilterCategory)
                {
                    return;
                }
                var weightedColor = GetWeightedColor(listing.Price);
                var itemLabel = new Label
                {
                    Text = listing.ListingName == string.Empty ? prototype.Name : listing.ListingName,
                    ToolTip = listing.Description == string.Empty ? prototype.Description : listing.Description,
                    HorizontalExpand = true,
                    Modulate = _loggedInUplinkAccount?.DataBalance >= listing.Price
                    ? Color.White
                    : Color.Gray.WithAlpha(0.30f)
                };

                var priceLabel = new Label
                {
                    Text = $"{listing.Price} TC",
                    HorizontalAlignment = HAlignment.Right,
                    Modulate = _loggedInUplinkAccount?.DataBalance >= listing.Price
                    ? weightedColor
                    : Color.Gray.WithAlpha(0.30f)
                };

                //Padding for the price lable.
                var pricePadding = new HBoxContainer
                {
                    MinSize = (32, 1),
                };

                //Contains the name of the item and its price. Used for spacing item name and price.
                var listingButtonHbox = new HBoxContainer
                {
                    Children =
                    {
                        itemLabel,
                        priceLabel,
                        pricePadding
                    }
                };

                var listingButtonPanelContainer = new PanelContainer
                {
                    Children =
                    {
                        listingButtonHbox
                    }
                };

                var pdaUplinkListingButton = new PDAUplinkItemButton(listing)
                {
                    Children =
                    {
                        listingButtonPanelContainer
                    }
                };
                pdaUplinkListingButton.OnPressed += args
                    => OnListingButtonPressed?.Invoke(args, pdaUplinkListingButton.ButtonListing);
                UplinkListingsContainer.AddChild(pdaUplinkListingButton);
            }

            public void ClearListings()
            {
                UplinkListingsContainer.Children.Clear();
            }

            private sealed class PDAUplinkItemButton : ContainerButton
            {
                public PDAUplinkItemButton(UplinkListingData data)
                {
                    ButtonListing = data;
                }

                public UplinkListingData ButtonListing { get; }
            }

            private sealed class PDAUplinkCategoryButton : Button
            {
                public UplinkCategory ButtonCategory;

            }
        }
    }
}
