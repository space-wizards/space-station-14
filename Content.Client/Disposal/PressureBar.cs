using Content.Shared.Disposal;
using Content.Shared.Disposal.Unit;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Disposal;

public sealed class PressureBar : ProgressBar
{
    public bool UpdatePressure(TimeSpan fullTime)
    {
        var currentTime = IoCManager.Resolve<IGameTiming>().CurTime;
        var pressure = (float) Math.Min(1.0f, 1.0f - (fullTime.TotalSeconds - currentTime.TotalSeconds) * SharedDisposalUnitSystem.PressurePerSecond);
        UpdatePressureBar(pressure);
        return pressure >= 1.0f;
    }

    private void UpdatePressureBar(float pressure)
    {
        Value = pressure;

        var normalized = pressure / MaxValue;

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
        if (normalized <= leftSideSize)
        {
            normalized /= leftSideSize; // Adjust range to 0.0 to 1.0
            finalHue = MathHelper.Lerp(leftHue, middleHue, normalized);
        }
        else
        {
            normalized = (normalized - leftSideSize) / rightSideSize; // Adjust range to 0.0 to 1.0.
            finalHue = MathHelper.Lerp(middleHue, rightHue, normalized);
        }

        // Check if null first to avoid repeatedly creating this.
        ForegroundStyleBoxOverride ??= new StyleBoxFlat();

        var foregroundStyleBoxOverride = (StyleBoxFlat) ForegroundStyleBoxOverride;
        foregroundStyleBoxOverride.BackgroundColor =
            Color.FromHsv(new Vector4(finalHue, saturation, value, alpha));
    }
}
