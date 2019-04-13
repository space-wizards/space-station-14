using System;
using Content.Shared.GameObjects.Components.Power;
using SS14.Client.GameObjects.Components.UserInterface;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.GameObjects.Components.UserInterface;
using SS14.Shared.IoC;
using SS14.Shared.Utility;

namespace Content.Client.GameObjects.Components.Power
{
    public class ApcBoundUserInterface : BoundUserInterface
    {
        private SS14Window _window;
        private BaseButton _breakerButton;
        private Label _externalPowerStateLabel;
        private ProgressBar _chargeBar;

        protected override void Open()
        {
            base.Open();

            _window = new ApcWindow(IoCManager.Resolve<IDisplayManager>());
            _window.OnClose += Close;
            _breakerButton = _window.Contents.GetChild<BaseButton>("Rows/Breaker/Breaker");
            _breakerButton.OnPressed += _ => SendMessage(new ApcToggleMainBreakerMessage());
            _externalPowerStateLabel = _window.Contents.GetChild<Label>("Rows/ExternalStatus/Status");
            _chargeBar = _window.Contents.GetChild<ProgressBar>("Rows/Charge/Charge");
            _window.AddToScreen();
        }

        public ApcBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ApcBoundInterfaceState) state;

            _breakerButton.Pressed = castState.MainBreaker;
            switch (castState.ApcExternalPower)
            {
                case ApcExternalPowerState.None:
                    _externalPowerStateLabel.Text = "None";
                    break;
                case ApcExternalPowerState.Low:
                    _externalPowerStateLabel.Text = "Low";
                    break;
                case ApcExternalPowerState.Good:
                    _externalPowerStateLabel.Text = "Good";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _chargeBar.Value = castState.Charge;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window.Dispose();
            }
        }

        private class ApcWindow : SS14Window
        {
            protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Power/Apc.tscn");

            public ApcWindow(IDisplayManager displayMan) : base(displayMan) { }
        }
    }
}
