using System;
using Content.Shared.GameObjects.Components.Power;
using OpenTK.Graphics.OpenGL4;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

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

            _window = new ApcWindow(IoCManager.Resolve<IDisplayManager>())
            {
                MarginRight = 426.0f, MarginBottom = 270.0f
            };
            _window.OnClose += Close;
            _window.AddToScreen();

            _breakerButton = _window.Contents.GetChild<CheckButton>("Rows/Breaker/Breaker");
            _breakerButton.OnPressed += _ => SendMessage(new ApcToggleMainBreakerMessage());

            _externalPowerStateLabel = _window.Contents.GetChild<Label>("Rows/ExternalStatus/Status");
            _chargeBar = _window.Contents.GetChild<ProgressBar>("Rows/Charge/Charge");
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
            public ApcWindow(IDisplayManager displayMan) : base(displayMan)
            {
                var rows = new VBoxContainer("Rows");

                var statusHeader = new Label("StatusHeader") { Text = "Power Status: " };
                rows.AddChild(statusHeader);

                var breaker = new HBoxContainer("Breaker");
                var breakerLabel = new Label("Label") { Text = "Main Breaker: " };
                var breakerButton = new CheckButton {Name = "Breaker"};
                breaker.AddChild(breakerLabel);
                breaker.AddChild(breakerButton);
                rows.AddChild(breaker);

                var externalStatus = new HBoxContainer("ExternalStatus");
                var externalStatusLabel = new Label("Label") { Text = "External Power: " };
                var externalPowerStateLabel = new Label("Status") { Text = "Good" };
                externalStatus.AddChild(externalStatusLabel);
                externalStatus.AddChild(externalPowerStateLabel);
                rows.AddChild(externalStatus);

                var charge = new HBoxContainer("Charge");
                var chargeLabel = new Label("Label") { Text = "Charge:" };
                var chargeBar = new ProgressBar("Charge")
                {
                    SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                    MinValue = 0.0f,
                    MaxValue = 1.0f,
                    Page = 0.0f,
                    Value = 0.5f
                };
                charge.AddChild(chargeLabel);
                charge.AddChild(chargeBar);
                rows.AddChild(charge);

                Contents.AddChild(rows);
            }
        }
    }
}
