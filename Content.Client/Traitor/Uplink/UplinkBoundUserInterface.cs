using Content.Client.Examine;
using Content.Client.Message;
using Content.Shared.Traitor.Uplink;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Traitor.Uplink
{
    [UsedImplicitly]
    public class UplinkBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        private UplinkMenu? _menu;
        private UplinkMenuPopup? _failPopup;

        public UplinkBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _menu = new UplinkMenu(_prototypeManager);
            _menu.OpenCentered();
            _menu.OnClose += Close;

            _menu.OnListingButtonPressed += (_, listing) =>
            {
                if (_menu.CurrentLoggedInAccount?.DataBalance < listing.Price)
                {
                    _failPopup = new UplinkMenuPopup(Loc.GetString("uplink-bound-user-interface-insufficient-funds-popup"));
                    _userInterfaceManager.ModalRoot.AddChild(_failPopup);
                    _failPopup.Open(UIBox2.FromDimensions(_menu.Position.X + 150, _menu.Position.Y + 60, 156, 24));
                    _menu.OnClose += () =>
                    {
                        _failPopup.Dispose();
                    };
                }

                SendMessage(new UplinkBuyListingMessage(listing.ItemId));
            };

            _menu.OnCategoryButtonPressed += (_, category) =>
            {
                _menu.CurrentFilterCategory = category;
                SendMessage(new UplinkRequestUpdateInterfaceMessage());

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
                case UplinkUpdateState msg:
                {
                    _menu.CurrentLoggedInAccount = msg.Account;
                    var balance = msg.Account.DataBalance;
                    string weightedColor = balance switch
                    {
                        <= 0 => "gray",
                        <= 5 => "green",
                        <= 20 => "yellow",
                        <= 50 => "purple",
                        _ => "gray"
                    };
                    _menu.BalanceInfo.SetMarkup(Loc.GetString("uplink-bound-user-interface-tc-balance-popup",
                                                              ("weightedColor", weightedColor),
                                                              ("balance", balance)));

                    _menu.ClearListings();
                    foreach (var item in
                        msg.Listings) //Should probably chunk these out instead. to-do if this clogs the internet tubes.
                    {
                        _menu.AddListingGui(item);
                    }

                    break;
                }
            }
        }

        private sealed class UplinkMenuPopup : Popup
        {
            public UplinkMenuPopup(string text)
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
    }
}
