using Content.Shared.GameObjects.Components.Disposal;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
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
        private readonly Label _pressurePercentage;
        public readonly Button Engage;
        public readonly Button Eject;
        public readonly Button Power;

        public DisposalUnitWindow()
        {
            MinSize = SetSize = (300, 200);

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = Loc.GetString("State: ")},
                            (_unitState = new Label {Text = Loc.GetString("Ready")})
                        }
                    },
                    new Control {MinSize = (0, 10)},
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = Loc.GetString("Pressure:")},
                            (_pressureBar = new ProgressBar
                            {
                                MinSize = (200, 20),
                                HorizontalAlignment = HAlignment.Right,
                                MinValue = 0,
                                MaxValue = 1,
                                Page = 0,
                                Value = 0.5f,
                                Children =
                                {
                                    (_pressurePercentage = new Label())
                                }
                            })
                        }
                    },
                    new Control {MinSize = (0, 10)},
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = Loc.GetString("Handle:")},
                            (Engage = new Button
                            {
                                Text = Loc.GetString("Engage"),
                                ToggleMode = true
                            })
                        }
                    },
                    new Control {MinSize = (0, 10)},
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = Loc.GetString("Eject:")},
                            (Eject = new Button {Text = Loc.GetString("Eject Contents")})
                        }
                    },
                    new Control {MinSize = (0, 10)},
                    new HBoxContainer
                    {
                        Children =
                        {
                            (Power = new CheckButton {Text = Loc.GetString("Power")}),
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

            var percentage = pressure / _pressureBar.MaxValue * 100;
            _pressurePercentage.Text = $" {percentage:0}%";
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
