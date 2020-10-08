#nullable enable
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects.Components.UserInterface;
using static Content.Shared.GameObjects.Components.Disposal.SharedDisposalMailingUnitComponent;

namespace Content.Client.GameObjects.Components.Disposal
{
    /// <summary>
    /// Initializes a <see cref="DisposalMailingUnitWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public class DisposalMailingUnitBoundUserInterface : BoundUserInterface
    {
        private DisposalMailingUnitWindow? _window;

        public DisposalMailingUnitBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private void ButtonPressed(UiButton button)
        {
            SendMessage(new UiButtonPressedMessage(button));
        }

        protected override void Open()
        {
            base.Open();

            _window = new DisposalMailingUnitWindow();

            _window.OpenCentered();
            _window.OnClose += Close;

            _window.Eject.OnPressed += _ => ButtonPressed(UiButton.Eject);
            _window.Engage.OnPressed += _ => ButtonPressed(UiButton.Engage);
            _window.Power.OnPressed += _ => ButtonPressed(UiButton.Power);
            _window.TargetListContainer.OnItemSelected += TargetSelected;

        }


        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (!(state is DisposalMailingUnitBoundUserInterfaceState cast))
            {
                return;
            }

            _window?.UpdateState(cast);
        }

        private void TargetSelected(ItemList.ItemListSelectedEventArgs item)
        {
            SendMessage(new UiTargetUpdateMessage(_window?.TargetList[item.ItemIndex]));
            //(ノ°Д°）ノ︵ ┻━┻
            if (_window != null) _window.Engage.Disabled = false;
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
