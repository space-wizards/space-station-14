using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Power;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.Graphics.Drawing;
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
                    _externalPowerStateLabel.SetOnlyStyleClass(NanoStyle.StyleClassPowerStateNone);
                    break;
                case SmesExternalPowerState.Low:
                    _externalPowerStateLabel.Text = "Low";
                    _externalPowerStateLabel.SetOnlyStyleClass(NanoStyle.StyleClassPowerStateLow);
                    break;
                case SmesExternalPowerState.Good:
                    _externalPowerStateLabel.Text = "Good";
                    _externalPowerStateLabel.SetOnlyStyleClass(NanoStyle.StyleClassPowerStateGood);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
    
            _chargeBar.Value = castState.Charge;
            UpdateChargeBarColor(castState.Charge);
            float ChargePercentage = (castState.Charge / _chargeBar.MaxValue) * 100.0f;
            _window.ChargePercentage.Text = " " + ChargePercentage.ToString("0.00") + "%";
        }

        private void UpdateChargeBarColor(float charge)
        {
            float normalizedCharge = charge / _chargeBar.MaxValue;

            float leftHue = 0.0f;// Red
            float middleHue = 0.066f;// Orange
            float rightHue = 0.33f;// Green
            float saturation = 1.0f;// Uniform saturation
            float value = 0.8f;// Uniform value / brightness
            float alpha = 1.0f;// Uniform alpha

            // These should add up to 1.0 or your transition won't be smooth
            float leftSideSize = 0.5f;// Fraction of _chargeBar lerped from leftHue to middleHue
            float rightSideSize = 0.5f;// Fraction of _chargeBar lerped from middleHue to rightHue

            float finalHue;
            if (normalizedCharge <= leftSideSize)
            {
                normalizedCharge /= leftSideSize;// Adjust range to 0.0 to 1.0
                finalHue = FloatMath.Lerp(leftHue, middleHue, normalizedCharge);
            }
            else
            {
                normalizedCharge = (normalizedCharge - leftSideSize) / rightSideSize;// Adjust range to 0.0 to 1.0.
                finalHue = FloatMath.Lerp(middleHue, rightHue, normalizedCharge);
            }

            // Check if null first to avoid repeatedly creating this.
            if (_chargeBar.ForegroundStyleBoxOverride == null)
            {
                _chargeBar.ForegroundStyleBoxOverride = new StyleBoxFlat();
            }

            var foregroundStyleBoxOverride = (StyleBoxFlat)_chargeBar.ForegroundStyleBoxOverride;
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
                ExternalPowerStateLabel.SetOnlyStyleClass(NanoStyle.StyleClassPowerStateGood);
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
