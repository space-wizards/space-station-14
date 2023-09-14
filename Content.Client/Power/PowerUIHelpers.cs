using Content.Client.Stylesheets;
using Content.Shared.Power;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Power
{
    public static class PowerUIHelpers
    {
        public static void FillExternalPowerLabel(Label label, ExternalPowerState externalPower)
        {
            switch (externalPower)
            {
                case ExternalPowerState.None:
                    label.Text = Loc.GetString("apc-menu-power-state-none");
                    label.SetOnlyStyleClass(StyleNano.StyleClassPowerStateNone);
                    break;
                case ExternalPowerState.Low:
                    label.Text = Loc.GetString("apc-menu-power-state-low");
                    label.SetOnlyStyleClass(StyleNano.StyleClassPowerStateLow);
                    break;
                case ExternalPowerState.Good:
                    label.Text = Loc.GetString("apc-menu-power-state-good");
                    label.SetOnlyStyleClass(StyleNano.StyleClassPowerStateGood);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void FillBatteryChargeProgressBar(ProgressBar bar, float charge)
        {
            bar.Value = charge;

            var normalizedCharge = charge / bar.MaxValue;

            const float leftHue = 0.0f; // Red
            const float middleHue = 0.066f; // Orange
            const float rightHue = 0.33f; // Green
            const float saturation = 1.0f; // Uniform saturation
            const float value = 0.8f; // Uniform value / brightness
            const float alpha = 1.0f; // Uniform alpha

            // These should add up to 1.0 or your transition won't be smooth
            const float leftSideSize = 0.5f; // Fraction of ChargeBar lerped from leftHue to middleHue
            const float rightSideSize = 0.5f; // Fraction of ChargeBar lerped from middleHue to rightHue

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
            bar.ForegroundStyleBoxOverride ??= new StyleBoxFlat();

            var foregroundStyleBoxOverride = (StyleBoxFlat) bar.ForegroundStyleBoxOverride;
            foregroundStyleBoxOverride.BackgroundColor =
                Color.FromHsv(new Vector4(finalHue, saturation, value, alpha));
        }
    }
}
