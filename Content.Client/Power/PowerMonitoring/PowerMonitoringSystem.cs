using Content.Client.Stylesheets;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using System.Linq;
using static Robust.Client.UserInterface.Control;

namespace Content.Client.Power.PowerMonitoring;

[UsedImplicitly]
public sealed class PowerMonitoringSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    public void UpdateExternalPowerStateLabel(Label externalPowerStateLabel, ExternalPowerState externalPowerState)
    {
        switch (externalPowerState)
        {
            case ExternalPowerState.None:
                externalPowerStateLabel.Text = Loc.GetString("power-monitoring-window-power-state-none");
                externalPowerStateLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateNone);
                break;
            case ExternalPowerState.Low:
                externalPowerStateLabel.Text = Loc.GetString("power-monitoring-window-power-state-low");
                externalPowerStateLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateLow);
                break;
            case ExternalPowerState.Stable:
                externalPowerStateLabel.Text = Loc.GetString("power-monitoring-window-power-state-stable");
                externalPowerStateLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateStable);
                break;
            case ExternalPowerState.Good:
                externalPowerStateLabel.Text = Loc.GetString("power-monitoring-window-power-state-good");
                externalPowerStateLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateGood);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void UpdateChargeBarColor(ProgressBar chargeBar, float charge)
    {
        if (chargeBar == null)
            return;

        var normalizedCharge = charge / chargeBar.MaxValue;

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
        chargeBar.ForegroundStyleBoxOverride ??= new StyleBoxFlat();

        var foregroundStyleBoxOverride = (StyleBoxFlat) chargeBar.ForegroundStyleBoxOverride;
        foregroundStyleBoxOverride.BackgroundColor =
            Color.FromHsv(new Vector4(finalHue, saturation, value, alpha));
    }

    public void UpdateSourcesList(GridContainer sourcesList, PowerMonitoringEntry[] sources)
    {
        UpdateSourceLoadList(sourcesList, sources);
    }

    public void UpdateLoadsList(GridContainer loadsList, PowerMonitoringEntry[] loads, bool showInactiveConsumers = false)
    {
        if (!showInactiveConsumers)
        {
            // Not showing inactive consumers, so hiding them.
            // This means filtering out loads that are not either:
            // + Batteries (always important)
            // + Meaningful (size above 0)
            loads = loads.Where(a => a.IsBattery || a.Size > 0.0f).ToArray();
        }

        UpdateSourceLoadList(loadsList, loads);
    }

    private void UpdateSourceLoadList(GridContainer list, PowerMonitoringEntry[] listVal)
    {
        // Remove excess children
        while (list.ChildCount > listVal.Length * 3)
        {
            list.RemoveChild(list.GetChild(list.ChildCount - 1));
        }

        // Add missing children
        while (list.ChildCount < listVal.Length * 3)
        {
            list.AddChild(new SpriteView());
            list.AddChild(new Label());
            list.AddChild(new Label());
        }

        // Update all remaining children
        for (int i = 0; i < listVal.Length; i++)
        {
            var ent = listVal[i];

            // Icon
            var icon = list.GetChild(3 * i) as SpriteView;

            if (icon != null)
            {
                icon.SetEntity(_entityManager.GetEntity(ent.NetEntity));
                icon.OverrideDirection = Direction.South;
                icon.SetSize = new System.Numerics.Vector2(32, 32);
                icon.Margin = new Thickness(10, 0, 0, 0);
            }

            // Entity name
            var label = list.GetChild(3 * i + 1) as Label;

            if (label != null)
            {
                label.Text = ent.NameLocalized;
                label.HorizontalExpand = true;
                label.HorizontalAlignment = HAlignment.Left;
                label.Margin = new Thickness(10, 0, 0, 0);
            }

            // Power value
            var power = list.GetChild(3 * i + 2) as Label;

            if (power != null)
            {
                power.Text = $"{Loc.GetString("power-monitoring-window-value", ("value", ent.Size))}";
                power.HorizontalExpand = true;
                power.HorizontalAlignment = HAlignment.Right;
                power.Margin = new Thickness(0, 0, 10, 0);
            }
        }
    }
}
