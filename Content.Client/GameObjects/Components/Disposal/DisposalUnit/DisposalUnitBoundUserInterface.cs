#nullable enable
using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.GameObjects.Components.Disposal.DisposalUnit;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using static Content.Shared.GameObjects.Components.Disposal.UiButtonPressedMessage;
using static Content.Shared.GameObjects.Components.Disposal.DisposalUnit.SharedDisposalUnitComponent;

namespace Content.Client.GameObjects.Components.Disposal.DisposalUnit
{
    /// <summary>
    /// Initializes a <see cref="DisposalUnitWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public class DisposalUnitBoundUserInterface : BoundUserInterface
    {
        private DisposalUnitWindow? _window;

        public DisposalUnitBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
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

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch(message)
            {
                case DisposalUnitPressureChangedMessage pressureChangedMessage:
                    PressureChanged(pressureChangedMessage.Pressure, pressureChangedMessage.TargetPressure);
                    break;
            }
        }

        protected override void Open()
        {
            base.Open();

            _window = new DisposalUnitWindow();

            _window.OpenCentered();
            _window.OnClose += Close;

            _window.Eject.OnPressed += _ => ButtonPressed(UiButton.Eject);
            _window.Engage.OnPressed += _ => ButtonPressed(UiButton.Engage);
            _window.Power.OnPressed += _ => ButtonPressed(UiButton.Power);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not DisposalUnitBoundUserInterfaceState cast)
            {
                return;
            }

            _window?.UpdateState(cast);
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
