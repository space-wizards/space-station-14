using Robust.Client.GameObjects;
using Content.Shared.PlayingCard;

namespace Content.Client.PlayingCard.UI
{
    /// <summary>
    /// Initializes a <see cref="PlayingCardDeckWindow"/> and updates it when new server messages are received.
    /// </summary>
    public class PlayingCardDeckBoundUserInterface : BoundUserInterface
    {
        private PlayingCardDeckWindow? _window;

        public PlayingCardDeckBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new PlayingCardDeckWindow();
            _window.OpenCentered();

            _window.PickupCards.OnPressed += _ => OnSubmit(_window.PickupCardAmount.Value);

            _window.OnClose += Close;
        }

        private void OnSubmit(int count)
        {
            SendMessage(new PickupCountMessage(count));
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }

}
