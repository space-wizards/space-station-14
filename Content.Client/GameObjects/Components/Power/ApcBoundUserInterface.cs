using System;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.GameObjects.Components.Power;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Power
{
    [UsedImplicitly]
    public class ApcBoundUserInterface : BoundUserInterface
    {
        private ApcWindow _window;
        private BaseButton _breakerButton;
        private Label _externalPowerStateLabel;
        private ProgressBar _chargeBar;

        protected override void Open()
        {
            base.Open();

            _window = new ApcWindow();
            _window.OnClose += Close;
            _window.OpenCentered();

            _breakerButton = _window.BreakerButton;
            _breakerButton.OnPressed += _ => SendMessage(new ApcToggleMainBreakerMessage());

            _externalPowerStateLabel = _window.ExternalPowerStateLabel;
            _chargeBar = _window.ChargeBar;
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
                    _externalPowerStateLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateNone);
                    break;
                case ApcExternalPowerState.Low:
                    _externalPowerStateLabel.Text = "Low";
                    _externalPowerStateLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateLow);
                    break;
                case ApcExternalPowerState.Good:
                    _externalPowerStateLabel.Text = "Good";
                    _externalPowerStateLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateGood);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _chargeBar.Value = castState.Charge;
            UpdateChargeBarColor(castState.Charge);
            var chargePercentage = (castState.Charge / _chargeBar.MaxValue) * 100.0f;
            _window.ChargePercentage.Text = " " + chargePercentage.ToString("0.00") + "%";
        }

        private void UpdateChargeBarColor(float charge)
        {
            var normalizedCharge = charge / _chargeBar.MaxValue;

            const float leftHue = 0.0f; // Red
            const float middleHue = 0.066f; // Orange
            const float rightHue = 0.33f; // Green
            const float saturation = 1.0f; // Uniform saturation
            const float value = 0.8f; // Uniform value / brightness
            const float alpha = 1.0f; // Uniform alpha

            // These should add up to 1.0 or your transition won't be smooth
            const float leftSideSize = 0.5f; // Fraction of _chargeBar lerped from leftHue to middleHue
            const float rightSideSize = 0.5f; // Fraction of _chargeBar lerped from middleHue to rightHue

            float finalHue;
            if (normalizedCharge <= leftSideSize)
            {
                normalizedCharge /= leftSideSize; // Adjust range to 0.0 to 1.0
                finalHue = MathHelper.Lerp(leftHue, middleHue, normalizedCharge);
            }
            else
            {
                normalizedCharge = (normalizedCharge - leftSideSize) / rightSideSize; // Adjust range to 0.0 to 1.0.
                finalHue = MathHelper.Lerp(middleHue, rightHue, normalizedCharge);
            }

            // Check if null first to avoid repeatedly creating this.
            if (_chargeBar.ForegroundStyleBoxOverride == null)
            {
                _chargeBar.ForegroundStyleBoxOverride = new StyleBoxFlat();
            }

            var foregroundStyleBoxOverride = (StyleBoxFlat) _chargeBar.ForegroundStyleBoxOverride;
            foregroundStyleBoxOverride.BackgroundColor =
                Color.FromHsv(new Vector4(finalHue, saturation, value, alpha));
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
            public Button BreakerButton { get; set; }
            public Label ExternalPowerStateLabel { get; set; }
            public ProgressBar ChargeBar { get; set; }
            public Label ChargePercentage { get; set; }

            public ApcWindow()
            {
                Title = "APC";
                var rows = new VBoxContainer();

                var statusHeader = new Label {Text = "Power Status: "};
                rows.AddChild(statusHeader);

                var breaker = new HBoxContainer();
                var breakerLabel = new Label {Text = "Main Breaker: "};
                BreakerButton = new CheckButton {Text = "Toggle"};
                breaker.AddChild(breakerLabel);
                breaker.AddChild(BreakerButton);
                rows.AddChild(breaker);

                var externalStatus = new HBoxContainer();
                var externalStatusLabel = new Label {Text = "External Power: "};
                ExternalPowerStateLabel = new Label {Text = "Good"};
                ExternalPowerStateLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateGood);
                externalStatus.AddChild(externalStatusLabel);
                externalStatus.AddChild(ExternalPowerStateLabel);
                rows.AddChild(externalStatus);

                var charge = new HBoxContainer();
                var chargeLabel = new Label {Text = "Charge:"};
                ChargeBar = new ProgressBar
                {
                    HorizontalExpand = true,
                    MinValue = 0.0f,
                    MaxValue = 1.0f,
                    Page = 0.0f,
                    Value = 0.5f
                };
                ChargePercentage = new Label();
                charge.AddChild(chargeLabel);
                charge.AddChild(ChargeBar);
                charge.AddChild(ChargePercentage);
                rows.AddChild(charge);

                Contents.AddChild(rows);
            }
        }
    }
}
