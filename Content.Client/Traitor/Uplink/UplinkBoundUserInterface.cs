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
                SendMessage(new UplinkBuyListingMessage(listing.ItemId));
            };

            _menu.OnCategoryButtonPressed += (_, category) =>
            {
                _menu.CurrentFilterCategory = category;
                SendMessage(new UplinkRequestUpdateInterfaceMessage());

            };

            _menu.OnWithdrawAttempt += (tc) =>
            {
                SendMessage(new UplinkTryWithdrawTC(tc));
            };
        }
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_menu == null)
                return;

            switch (state)
            {
                case UplinkUpdateState msg:
                    _menu.UpdateAccount(msg.Account);
                    _menu.UpdateListing(msg.Listings);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Close();
            _menu?.Dispose();
        }
    }
}
