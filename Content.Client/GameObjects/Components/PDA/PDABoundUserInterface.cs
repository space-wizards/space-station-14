using System;
using System.Collections.Generic;
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
using Robust.Shared.Log;
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
                    SendMessage(new PDARequestUplinkListingsMessage());
                }
            };

            _menu.OnListingButtonPressed += args =>
            {
                if (!_menu.UplinkListingDataButtons.TryGetValue(args.Button, out var listing))
                {
                    return;
                }

                SendMessage(new PDAUplinkBuyListingMessage(listing));
            };

            _menu.OnCategoryButtonPressed += (args, category) =>
            {
                _menu.CurrentFilterCategory = category;
                SendMessage(new PDARequestUplinkListingsMessage());
            };
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            DebugTools.Assert((state is PDAUBoundUserInterfaceState));

            var cstate = (PDAUBoundUserInterfaceState) state;
            switch (state)
            {
                case PDAUpdateMainMenuState msg:
                {
                    _menu.FlashLightToggleButton.Pressed = msg.FlashlightEnabled;
                    _menu.PDAOwnerLabel.SetMarkup(Loc.GetString("Owner: [color=white]{0}[/color]",
                        msg.PDAOwnerInfo.ActualOwnerName));

                    if (msg.PDAOwnerInfo.JobTitle == null || msg.PDAOwnerInfo.IDOwner == null)
                    {
                        _menu.IDInfoLabel.SetMarkup(Loc.GetString("ID:"));
                    }
                    else
                    {
                        _menu.IDInfoLabel.SetMarkup(Loc.GetString(
                            "ID: [color=white]{0}[/color], [color=yellow]{1}[/color]",
                            msg.PDAOwnerInfo.IDOwner,
                            msg.PDAOwnerInfo.JobTitle));
                    }

                    _menu.EjectIDButton.Visible = msg.PDAOwnerInfo.IDOwner != null;
                    break;
                }

                case PDASendUplinkListingsMessage msg:
                {
                    _menu.ClearListings();
                    foreach (var item in msg.Listings)
                    {
                        _menu.AddListingGUI(item);
                    }

                    break;
                }

                case PDAUplinkAccountLoginMessage loginMsg:
                {
                    _menu.LoggedInAccount = loginMsg.Account;
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

            public Dictionary<BaseButton, UplinkListingData> UplinkListingDataButtons;
            public event Action<BaseButton.ButtonEventArgs> OnListingButtonPressed;
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

            public UplinkAccount LoggedInAccount
            {
                get => _loggedInAccount;
                set => _loggedInAccount = value;
            }


            private UplinkCategory _currentFilter;
            private UplinkAccount _loggedInAccount;


            public PDAMenu(PDABoundUserInterface owner, IPrototypeManager prototypeManager)
            {
                _owner = owner;
                _prototypeManager = prototypeManager;
                Title = Loc.GetString("PDA");
                UplinkListingDataButtons = new Dictionary<BaseButton, UplinkListingData>();


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


                var _uplinkShopScrollContainer = new ScrollContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    SizeFlagsStretchRatio = 2,
                    CustomMinimumSize = (100, 256)
                };

                //Add the category list to the left side. The store to the right.
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
                CategoryAndListingsContainer.AddChild(_uplinkShopScrollContainer);
                masterPanelContainer.AddChild(CategoryAndListingsContainer);

                //Actual list of buttons.
                UplinkListingsContainer = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    SizeFlagsStretchRatio = 2,
                    CustomMinimumSize = (100, 256),
                };
                _uplinkShopScrollContainer.AddChild(UplinkListingsContainer);

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
                PopulateCategories();
                #endregion

                MasterTabContainer = new TabContainer
                {
                    Children =
                    {
                        mainMenuTabContainer,
                        UplinkTabContainer
                    }
                };

                MasterTabContainer.SetTabTitle(0, Loc.GetString("Main Menu"));
                MasterTabContainer.SetTabTitle(1, Loc.GetString("Uplink -DEBUG-"));
                Contents.AddChild(MasterTabContainer);
            }

            private void PopulateCategories()
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

            public void AddListingGUI(UplinkListingData listing)
            {
                if (!_prototypeManager.TryIndex(listing.ItemID, out EntityPrototype prototype) || listing.Category != CurrentFilterCategory)
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


                //lmao
                var itemColor = Color.White;
                if (listing.Price <= 5)
                {
                    itemColor = Color.White;
                }
                else if (listing.Price >= 10 && listing.Price < 30)
                {
                    itemColor = Color.Yellow;
                }
                else if (listing.Price >= 30 && listing.Price < 50)
                {
                    itemColor = Color.Orange;
                }
                else
                {
                    itemColor = Color.Cyan;
                }


                var hbox = new HBoxContainer
                {
                    Modulate = itemColor,
                    Children =
                    {
                        itemLabel,
                        priceLabel
                    }
                };

                var hboxPanelBG = new PanelContainer
                {
                    Children =
                    {
                        hbox
                    }
                };

                var button = new PDAUplinkItemButton
                {
                    SizeFlagsVertical = SizeFlags.Fill,
                    Children =
                    {
                        hboxPanelBG
                    }
                };

                button.OnPressed += args => OnListingButtonPressed?.Invoke(args);
                UplinkListingsContainer.AddChild(button);
                UplinkListingDataButtons.Add(button, listing);
            }


            public void ClearListings()
            {
                UplinkListingsContainer.Children.Clear();
            }

            private sealed class PDAUplinkItemButton : ContainerButton
            {
            }

            public class PDAUplinkCategoryButton : Button
            {
                public UplinkCategory ButtonCategory;

            }
        }
    }
}
