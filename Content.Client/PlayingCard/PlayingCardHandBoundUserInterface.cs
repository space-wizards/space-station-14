using Content.Client.PlayingCard.UI;
using Content.Shared.PlayingCard;
using Robust.Client.GameObjects;
using static Content.Shared.PlayingCard.SharedPlayingCardHandComponent;

namespace Content.Client.PlayingCard
{
    public class PlayingCardHandBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private PlayingCardHandMenu? _menu;

        public SharedPlayingCardHandComponent? PlayingCardHand { get; private set; }

        public PlayingCardHandBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new CardListSyncRequestMessage());
        }

        protected override void Open()
        {
            base.Open();

            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent(Owner.Owner, out SharedPlayingCardHandComponent? playingCardHand))
            {
                return;
            }

            PlayingCardHand = playingCardHand;

            _menu = new PlayingCardHandMenu(this) {Title = entMan.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};
            _menu.Populate(PlayingCardHand.CardList);

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        public void PickCard(string id)
        {
            SendMessage(new PickSingleCardMessage(id));
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case CardListMessage msg:
                    _menu?.Populate(msg.Cards);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
}
