using System;
using Content.Shared.PDA;
using Content.Shared.Traitor.Uplink;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Traitor.Uplink
{
    public class UplinkMenu : SS14Window
    {
        public BoxContainer UplinkTabContainer { get; }

        protected readonly HSplitContainer CategoryAndListingsContainer;

        private readonly IPrototypeManager _prototypeManager;

        public readonly BoxContainer UplinkListingsContainer;

        public readonly BoxContainer CategoryListContainer;
        public readonly RichTextLabel BalanceInfo;
        public event Action<BaseButton.ButtonEventArgs, UplinkListingData>? OnListingButtonPressed;
        public event Action<BaseButton.ButtonEventArgs, UplinkCategory>? OnCategoryButtonPressed;

        public UplinkMenu(IPrototypeManager prototypeManager)
        {
            _prototypeManager = prototypeManager;

            MinSize = SetSize = (512, 512);
            Title = Loc.GetString("pda-bound-user-interface-uplink-tab-title");

            //Uplink Tab
            CategoryListContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical
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
            UplinkListingsContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
                VerticalExpand = true,
                SizeFlagsStretchRatio = 2,
                MinSize = (100, 256),
            };
            uplinkShopScrollContainer.AddChild(UplinkListingsContainer);

            var innerVboxContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                VerticalExpand = true,

                Children =
                {
                    BalanceInfo,
                    masterPanelContainer
                }
            };

            UplinkTabContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Children =
                {
                    innerVboxContainer
                }
            };
            PopulateUplinkCategoryButtons();

            Contents.AddChild(UplinkTabContainer);
        }

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

        public void AddListingGui(UplinkListingData listing)
        {
            if (!_prototypeManager.TryIndex(listing.ItemId, out EntityPrototype? prototype) || listing.Category != CurrentFilterCategory)
            {
                return;
            }
            var weightedColor = listing.Price switch
            {
                <= 0 => Color.Gray,
                <= 5 => Color.Green,
                <= 10 => Color.Yellow,
                <= 20 => Color.Orange,
                <= 50 => Color.Purple,
                _ => Color.Gray
            };
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
            var pricePadding = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                MinSize = (32, 1),
            };

            //Contains the name of the item and its price. Used for spacing item name and price.
            var listingButtonHbox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
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
