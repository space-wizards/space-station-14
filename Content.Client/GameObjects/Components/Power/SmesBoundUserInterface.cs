using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.GameObjects.Components.Power;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Power
{
    class SmesBoundUserInterface : BoundUserInterface
    {
        private SmesWindow _window;
        private BaseButton _breakerButton;
        private Label _externalPowerStateLabel;
        private ProgressBar _chargeBar;

        public SmesBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new SmesWindow()
            {
                MarginRight = 426.0f,
                MarginBottom = 270.0f
            };
            _window.OnClose += Close;
            _window.AddToScreen();

            _breakerButton = _window.BreakerButton;
            _breakerButton.OnPressed += _ => SendMessage(new SmesToggleMainBreakerMessage());

            _externalPowerStateLabel = _window.ExternalPowerStateLabel;
            _chargeBar = _window.ChargeBar;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (SmesBoundInterfaceState)state;

            _breakerButton.Pressed = castState.MainBreaker;
            switch (castState.SmesExternalPower)
            {
                case SmesExternalPowerState.None:
                    _externalPowerStateLabel.Text = "None";
                    _externalPowerStateLabel.FontColorOverride = new Color(0.8f, 0.0f, 0.0f);
                    break;
                case SmesExternalPowerState.Low:
                    _externalPowerStateLabel.Text = "Low";
                    _externalPowerStateLabel.FontColorOverride = new Color(0.9f, 0.36f, 0.0f);
                    break;
                case SmesExternalPowerState.Good:
                    _externalPowerStateLabel.Text = "Good";
                    _externalPowerStateLabel.FontColorOverride = new Color(0.024f, 0.8f, 0.0f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
    
            _chargeBar.Value = castState.Charge;
            float ChargePercentage = (castState.Charge / _chargeBar.MaxValue) * 100.0f;
            _window.ChargePercentage.Text = " " + ChargePercentage.ToString("0.00") + "%";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window.Dispose();
            }
        }

        private class SmesWindow : SS14Window
        {
            public Button BreakerButton { get; set; }
            public Label ExternalPowerStateLabel { get; set; }
            public ProgressBar ChargeBar { get; set; }
            public Label ChargePercentage { get; set; }

            public SmesWindow()
            {
                Title = "SMES";
                var rows = new VBoxContainer("Rows");

                var statusHeader = new Label("StatusHeader") { Text = "Power Status: " };
                rows.AddChild(statusHeader);

                var breaker = new HBoxContainer("Breaker");
                var breakerLabel = new Label("Label") { Text = "Main Breaker: " };
                BreakerButton = new CheckButton { Name = "Breaker", Text = "Toggle" };
                breaker.AddChild(breakerLabel);
                breaker.AddChild(BreakerButton);
                rows.AddChild(breaker);

                var externalStatus = new HBoxContainer("ExternalStatus");
                var externalStatusLabel = new Label("Label") { Text = "External Power: " };
                ExternalPowerStateLabel = new Label("Status") { Text = "Good" };
                externalStatus.AddChild(externalStatusLabel);
                externalStatus.AddChild(ExternalPowerStateLabel);
                rows.AddChild(externalStatus);

                var charge = new HBoxContainer("Charge");
                var chargeLabel = new Label("Label") { Text = "Charge:" };
                ChargeBar = new ProgressBar("Charge")
                {
                    SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                    MinValue = 0.0f,
                    MaxValue = 1.0f,
                    Page = 0.0f,
                    Value = 0.5f
                };
                ChargePercentage = new Label("ChargePercentage");
                charge.AddChild(chargeLabel);
                charge.AddChild(ChargeBar);
                charge.AddChild(ChargePercentage);

                rows.AddChild(charge);

                Contents.AddChild(rows);
            }
        }
    }
}
