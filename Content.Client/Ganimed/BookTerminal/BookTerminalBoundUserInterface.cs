using Content.Shared.Ganimed;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Ganimed.BookTerminal
{
    [UsedImplicitly]
    public sealed class BookTerminalBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private BookTerminalWindow? _window;

        [ViewVariables]
        private BookTerminalBoundUserInterfaceState? _lastState;

        public BookTerminalBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new()
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName,
            };

            _window.EjectButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent("bookSlot"));
            _window.ClearButton.OnPressed += _ => SendMessage(new BookTerminalClearContainerMessage());
            _window.UploadButton.OnPressed += _ => SendMessage(new BookTerminalUploadMessage());
            _window.CopyPasteButton.OnPressed += _ => SendMessage(new BookTerminalCopyPasteMessage());
            
			
			_window.OpenCentered();
            _window.OnClose += Close;
			
			_window.OnPrintBookButtonPressed += (args, button) => SendMessage(new BookTerminalPrintBookMessage(button.BookEntry));
            _window.OnPrintBookButtonMouseEntered += (args, button) =>
            {
                if (_lastState is not null)
                    _window.UpdateContainerInfo(_lastState);
            };
            _window.OnPrintBookButtonMouseExited += (args, button) =>
            {
                if (_lastState is not null)
                    _window.UpdateContainerInfo(_lastState);
            };
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (BookTerminalBoundUserInterfaceState) state;
            _lastState = castState;

            _window?.UpdateState(castState);
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
