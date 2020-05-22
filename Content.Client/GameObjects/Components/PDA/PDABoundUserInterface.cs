using System;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.PDA;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.Components.PDA
{
    public class PDABoundUserInterface : BoundUserInterface
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649
        private PDAMenu _menu;
        private ClientUserInterfaceComponent Owner;

        public PDABoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            Owner = owner;
        }

        protected override void Open()
        {
            base.Open();
            SendMessage(new PDARequestUpdateInterfaceMessage());
            _menu = new PDAMenu(this, _prototypeManager);
            _menu.OpenToLeft();
            _menu.OnClose += Close;
            _menu.FlashLightToggleButton.OnToggled += args =>
            {
                SendMessage(new PDAToggleFlashlightMessage());
            };

            _menu.EjectIDButton.OnPressed += args =>
            {
                SendMessage(new PDAEjectIDMessage());
            };

            _menu.MasterTabContainer.OnTabChanged += i =>
            {
                var tab = _menu.MasterTabContainer.GetChild(i);
                if (tab == _menu.UplinkTabContainer)
                {
                    SendMessage(new PDARequestUpdateInterfaceMessage());
                }
            };

            _menu.OnListingButtonPressed += (args, listing) =>
            {
                SendMessage(new PDAUplinkBuyListingMessage(listing));
            };

            _menu.OnCategoryButtonPressed += (args, category) =>
            {
                _menu.CurrentFilterCategory = category;
                SendMessage(new PDARequestUpdateInterfaceMessage());

            };
        }


        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            DebugTools.Assert((state is PDAUBoundUserInterfaceState));

            var cstate = (PDAUBoundUserInterfaceState) state;
            switch (state)
            {
                case PDAUpdateState msg:
                {
                    _menu.FlashLightToggleButton.Pressed = msg.FlashlightEnabled;
                    _menu.PDAOwnerLabel.SetMarkup(Loc.GetString("Owner: [color=white]{0}[/color]",
                        msg.PDAOwnerInfo.ActualOwnerName));

                    if (msg.PDAOwnerInfo.JobTitle == null || msg.PDAOwnerInfo.IdOwner == null)
                    {
                        _menu.IDInfoLabel.SetMarkup(Loc.GetString("ID:"));
                    }
                    else
                    {
                        _menu.IDInfoLabel.SetMarkup(Loc.GetString(
                            "ID: [color=white]{0}[/color], [color=yellow]{1}[/color]",
                            msg.PDAOwnerInfo.IdOwner,
                            msg.PDAOwnerInfo.JobTitle));
                    }

                    _menu.EjectIDButton.Visible = msg.PDAOwnerInfo.IdOwner != null;
                    if (msg.Account != null)
                    {
                        _menu.CurrentLoggedInAccount = msg.Account;
                    }

                    if (msg.Listings != null)
                    {
                        _menu.ClearListings();
                        foreach (var item in msg.Listings) //Should probably chunk these out instead. to-do if this clogs the internet tubes.
                        {
                            _menu.AddListingGui(item);
                        }
                    }
                    break;
                }

            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _menu?.Dispose();
        }

        private class PDAMenu : SS14Window
        {
            protected override Vector2? CustomSize => (512, 256);

            private PDABoundUserInterface _owner { get; }

            public Button FlashLightToggleButton { get; }
            public Button EjectIDButton { get; }

            public TabContainer MasterTabContainer;

            public RichTextLabel PDAOwnerLabel { get; }
            public PanelContainer IDInfoContainer { get; }
            public RichTextLabel IDInfoLabel { get; }

            public VBoxContainer UplinkTabContainer { get; }

            protected HSplitContainer CategoryAndListingsContainer;

            private IPrototypeManager _prototypeManager;

            public VBoxContainer UplinkListingsContainer;

            public VBoxContainer CategoryListContainer;
            public event Action<BaseButton.ButtonEventArgs, UplinkListingData> OnListingButtonPressed;
            public event Action<BaseButton.ButtonEventArgs, UplinkCategory> OnCategoryButtonPressed;

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

            public UplinkAccountData CurrentLoggedInAccount
            {
                get => _loggedInUplinkAccount;
                set => _loggedInUplinkAccount = value;
            }


            private UplinkCategory _currentFilter;
            private UplinkAccountData _loggedInUplinkAccount;


            public PDAMenu(PDABoundUserInterface owner, IPrototypeManager prototypeManager)
            {
                _owner = owner;
                _prototypeManager = prototypeManager;
                Title = Loc.GetString("PDA");

                #region MAIN_MENU_TAB
                //Main menu
                PDAOwnerLabel = new RichTextLabel
                {
                };

                IDInfoLabel = new RichTextLabel()
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                };

                EjectIDButton = new Button
                {
                    Text = Loc.GetString("Eject ID"),
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter
                };

                var innerHBoxContainer = new HBoxContainer
                {
                    Children =
                    {
                        IDInfoLabel,
                        EjectIDButton
                    }
                };

                IDInfoContainer = new PanelContainer
                {
                    SizeFlagsHorizontal = SizeFlags.Fill,
                    Children =
                    {
                        innerHBoxContainer,
                    }
                };

                FlashLightToggleButton = new Button
                {
                    Text = Loc.GetString("Toggle Flashlight"),
                    ToggleMode = true,
                };

                var mainMenuTabContainer = new VBoxContainer
                {
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    CustomMinimumSize = (50, 50),

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
                var uplinkStoreHeader = new Label
                {
                    Align = Label.AlignMode.Center,
                    Text = Loc.GetString("Uplink Listings"),
                };

                //Red background container.
                var masterPanelContainer = new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.DarkRed.WithAlpha(0.6f)},
                    SizeFlagsVertical = SizeFlags.FillExpand
                };

                //This contains both the panel of the category buttons and the listings box.
                CategoryAndListingsContainer = new HSplitContainer
                {
                    SizeFlagsVertical = SizeFlags.FillExpand,
                };


                var uplinkShopScrollContainer = new ScrollContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    SizeFlagsStretchRatio = 2,
                    CustomMinimumSize = (100, 256)
                };

                //Add the category list to the left side. The store items to center.
                var categoryListContainerBackground = new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.Black.WithAlpha(0.4f)},
                    SizeFlagsVertical = SizeFlags.FillExpand,
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
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    SizeFlagsStretchRatio = 2,
                    CustomMinimumSize = (100, 256),
                };
                uplinkShopScrollContainer.AddChild(UplinkListingsContainer);

                var innerVboxContainer = new VBoxContainer
                {
                    SizeFlagsVertical = SizeFlags.FillExpand,

                    Children =
                    {
                        uplinkStoreHeader,
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
                MasterTabContainer.SetTabTitle(0, Loc.GetString("Main Menu"));
                MasterTabContainer.AddChild(UplinkTabContainer);
                MasterTabContainer.SetTabTitle(1, Loc.GetString("Uplink"));
                Contents.AddChild(MasterTabContainer);
            }

            private void PopulateUplinkCategoryButtons()
            {

                foreach (UplinkCategory cat in Enum.GetValues(typeof (UplinkCategory)))
                {

                    var catButton = new PDAUplinkCategoryButton
                    {
                        Text = Loc.GetString(cat.ToString()),
                        ButtonCategory = cat

                    };

                    catButton.OnPressed += args => OnCategoryButtonPressed?.Invoke(args, catButton.ButtonCategory);

                    CategoryListContainer.AddChild(catButton);
                }

            }

            public void AddListingGui(UplinkListingData listing)
            {
                if (!_prototypeManager.TryIndex(listing.ItemId, out EntityPrototype prototype) || listing.Category != CurrentFilterCategory)
                {
                    return;
                }

                var itemLabel = new Label
                {
                    Text = listing.ListingName == string.Empty ? prototype.Name : listing.ListingName,
                    ToolTip = listing.Description == string.Empty ? prototype.Description : listing.Description,
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                };

                var priceLabel = new Label
                {
                    Text = $"{listing.Price} TC",
                    Align = Label.AlignMode.Right,
                };


                //Can the account afford this item? If so use the item's color, else gray it out.
                var itemColor = _loggedInUplinkAccount.DataBalance >= listing.Price
                    ? listing.DisplayColor
                    : Color.Gray.WithAlpha(0.25f);

                //Contains the name of the item and its price. Used for spacing price and name.
                var listingButtonHbox = new HBoxContainer
                {
                    Modulate = itemColor,
                    Children =
                    {
                        itemLabel,
                        priceLabel
                    }
                };

                var listingButtonPanelContainer = new PanelContainer
                {
                    Children =
                    {
                        listingButtonHbox
                    }
                };

                var pdaUplinkListingButton = new PDAUplinkItemButton
                {
                    ButtonListing = listing,
                    SizeFlagsVertical = SizeFlags.Fill,
                    Children =
                    {
                        listingButtonPanelContainer
                    }
                };
                pdaUplinkListingButton.OnPressed += args
                    => OnListingButtonPressed?.Invoke(args,pdaUplinkListingButton.ButtonListing);
                UplinkListingsContainer.AddChild(pdaUplinkListingButton);
            }


            public void ClearListings()
            {
                UplinkListingsContainer.Children.Clear();
            }


            private sealed class PDAUplinkItemButton : ContainerButton
            {
                public UplinkListingData ButtonListing;
            }

            private sealed class PDAUplinkCategoryButton : Button
            {
                public UplinkCategory ButtonCategory;

            }
        }
    }
}
