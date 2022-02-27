using Content.Shared.PlayingCard;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client.PlayingCard.UI;
    public class PlayingCardHandBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private PlayingCardHandMenu? _menu;

        private PlayingCardHandBoundUserInterfaceState? _lastState;

        public PlayingCardHandBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var entMan = IoCManager.Resolve<IEntityManager>();

            if(State != null)
                UpdateState(State);

            _menu = new PlayingCardHandMenu(this) {Title = entMan.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};

            // _menu.Populate(PlayingCardHand.CardList);

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_menu == null || state is not PlayingCardHandBoundUserInterfaceState cast)
                return;

            _lastState = cast;

            _menu?.Populate(cast.CardList);
        }

        public void RemoveCard(int id)
        {
            SendMessage(new PickSingleCardMessage(id));
        }

        // protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        // {
        //     switch (message)
        //     {
        //         case CardListMessage msg:
        //             // _menu?.Populate(msg.Cards);
        //             break;
        //     }
        // }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
