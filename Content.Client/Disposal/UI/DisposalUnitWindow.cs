using Content.Client.UserInterface.Stylesheets;
using Content.Shared.GameObjects.Components.Disposal;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.Disposal.SharedDisposalUnitComponent;

namespace Content.Client.GameObjects.Components.Disposal
{
    /// <summary>
    /// Client-side UI used to control a <see cref="SharedDisposalUnitComponent"/>
    /// </summary>
    public class DisposalUnitWindow : SS14Window
    {
        private readonly Label _unitState;
        private readonly ProgressBar _pressureBar;
        public readonly Button Engage;
        public readonly Button Eject;
        public readonly Button Power;

        public DisposalUnitWindow()
        {
            IoCManager.InjectDependencies(this);
            MinSize = SetSize = (300, 140);
            Resizable = false;
            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    new HBoxContainer
                    {
                        SeparationOverride = 4,
                        Children =
                        {
                            new Label {Text = Loc.GetString("ui-disposal-unit-label-state")},
                            (_unitState = new Label {Text = Loc.GetString("ui-disposal-unit-label-status")})
                        }
                    },
                    new Control {MinSize = (0, 5)},
                    new HBoxContainer
                    {
                        SeparationOverride = 4,
                        Children =
                        {
                            new Label {Text = Loc.GetString("ui-disposal-unit-label-pressure")},
                            (_pressureBar = new ProgressBar
                            {
                                MinSize = (190, 20),
                                HorizontalAlignment = HAlignment.Right,
                                MinValue = 0,
                                MaxValue = 1,
                                Page = 0,
                                Value = 0.5f
                            })
                        }
                    },
                    new Control {MinSize = (0, 10)},
                    new HBoxContainer
                    {
                        Children =
                        {
                            (Engage = new Button
                            {
                                Text = Loc.GetString("ui-disposal-unit-button-flush"),
                                StyleClasses = {StyleBase.ButtonOpenRight},
                                ToggleMode = true
                            }),

                            (Eject = new Button
                            {
                                Text = Loc.GetString("ui-disposal-unit-button-eject"),
                                StyleClasses = {StyleBase.ButtonOpenBoth}
                            }),

                            (Power = new CheckButton
                            {
                                Text = Loc.GetString("ui-disposal-unit-button-power"),
                                StyleClasses = {StyleBase.ButtonOpenLeft}
                            })

                        }
                    }
                }
            });
        }

        private void UpdatePressureBar(float pressure)
        {
            _pressureBar.Value = pressure;

            var normalized = pressure / _pressureBar.MaxValue;

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
            _pressureBar.ForegroundStyleBoxOverride ??= new StyleBoxFlat();

            var foregroundStyleBoxOverride = (StyleBoxFlat) _pressureBar.ForegroundStyleBoxOverride;
            foregroundStyleBoxOverride.BackgroundColor =
                Color.FromHsv(new Vector4(finalHue, saturation, value, alpha));
        }

        public void UpdateState(DisposalUnitBoundUserInterfaceState state)
        {
            Title = state.UnitName;
            _unitState.Text = state.UnitState;
            UpdatePressureBar(state.Pressure);
            Power.Pressed = state.Powered;
            Engage.Pressed = state.Engaged;
        }
    }
}
