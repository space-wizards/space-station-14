#nullable enable
using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.GameObjects.Components.Disposal.DisposalUnit;
using Content.Shared.GameObjects.Components.Disposal.MailingUnit;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects.Components.UserInterface;
using static Content.Shared.GameObjects.Components.Disposal.UiButtonPressedMessage;

namespace Content.Client.GameObjects.Components.Disposal
{
    /// <summary>
    /// Initializes a <see cref="MailingUnitWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public class MailingUnitBoundUserInterface : BoundUserInterface
    {
        private MailingUnitWindow? _window;

        public MailingUnitBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new MailingUnitWindow();

            _window.OpenCentered();
            _window.OnClose += Close;

            _window.Eject.OnPressed += _ => ButtonPressed(UiButton.Eject);
            _window.Engage.OnPressed += _ => ButtonPressed(UiButton.Engage);
            _window.Power.OnPressed += _ => ButtonPressed(UiButton.Power);
            _window.TargetListContainer.OnItemSelected += TargetSelected;

        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case DisposalUnitPressureChangedMessage pressureChangedMessage:
                    PressureChanged(pressureChangedMessage.Pressure, pressureChangedMessage.TargetPressure);
                    break;
            }
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not MailingUnitBoundUserInterfaceState cast)
            {
                return;
            }

            _window?.UpdateState(cast);
        }

        private void ButtonPressed(UiButton button)
        {
            SendMessage(new UiButtonPressedMessage(button));
        }

        private void PressureChanged(float pressure, float target)
        {
            float percentage;

            if (target == 0 || pressure > target)
            {
                percentage = 1f;
            }
            else
            {
                percentage = pressure / target;
            }


            _window?.UpdatePressureBar(percentage);
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
