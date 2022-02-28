using Content.Shared.PlayingCard;
using Robust.Client.GameObjects;

namespace Content.Client.PlayingCard.UI;
    public class PlayingCardHandBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private PlayingCardHandWindow? _window;

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

            _window = new PlayingCardHandWindow(this) {Title = entMan.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};

            _window.OnClose += Close;
            _window.OpenCentered();
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not PlayingCardHandBoundUserInterfaceState cast)
                return;

            _lastState = cast;

            _window?.Populate(cast.CardList);
        }

        public void RemoveCard(int id)
        {
            SendMessage(new PickSingleCardMessage(id));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _window?.Dispose();
        }
    }
