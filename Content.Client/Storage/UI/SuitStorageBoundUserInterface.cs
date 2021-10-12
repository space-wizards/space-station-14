using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.Storage.SharedSuitStorageComponent;

namespace Content.Client.Storage.UI
{
    [UsedImplicitly]
    public class SuitStorageBoundUserInterface : BoundUserInterface
    {
        public SuitStorageBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private SuitStorageWindow? _window;

        protected override void Open()
        {
            base.Open();


            _window = new SuitStorageWindow(new Dictionary<int, string?>());
            _window.OnClose += Close;
            _window.OpenStorageButton.OnPressed += _ =>
            {
                SendMessage(new SuitStorageUiButtonPressedMessage(UiButton.Open));
            };
            _window.CloseStorageButton.OnPressed += _ =>
            {
                SendMessage(new SuitStorageUiButtonPressedMessage(UiButton.Close));
            };
            _window.DispenseButton.OnPressed += _ =>
            {
                if (_window.SelectedItem != null)
                {
                    SendMessage(new SuitStorageUiButtonPressedMessage(UiButton.Dispense, (int) _window.SelectedItem));
                }
            };
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            _window?.Populate((SuitStorageBoundUserInterfaceState) state);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}
