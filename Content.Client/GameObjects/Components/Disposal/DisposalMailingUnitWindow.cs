using Content.Shared.GameObjects.Components.Disposal;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using System.Collections.Generic;
using Robust.Client.Graphics;
using static Content.Shared.GameObjects.Components.Disposal.SharedDisposalMailingUnitComponent;

namespace Content.Client.GameObjects.Components.Disposal
{
    /// <summary>
    /// Client-side UI used to control a <see cref="SharedDisposalMailingUnitComponent"/>
    /// </summary>
    public class DisposalMailingUnitWindow : SS14Window
    {
        private readonly Label _unitState;
        private readonly ProgressBar _pressureBar;
        private readonly Label _pressurePercentage;
        public readonly Button Engage;
        public readonly Button Eject;
        public readonly Button Power;

        public readonly ItemList TargetListContainer;
        public List<string> TargetList;
        private readonly Label _tagLabel;

        protected override Vector2? CustomSize => (460, 220);

        public DisposalMailingUnitWindow()
        {
            TargetList = new List<string>();
            Contents.AddChild(new HBoxContainer
            {
                Children =
                {
                    new MarginContainer
                    {
                        MarginLeftOverride = 8,
                        MarginRightOverride = 8,
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        Children =
                        {
                            new VBoxContainer
                            {
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                Children =
                                {
                                    new HBoxContainer
                                    {
                                        Children =
                                        {
                                            new Label {Text = Loc.GetString("State: ")},
                                            new Control {CustomMinimumSize = (4, 0)},
                                            (_unitState = new Label {Text = Loc.GetString("Ready")})
                                        }
                                    },
                                    new Control {CustomMinimumSize = (0, 10)},
                                    new HBoxContainer
                                    {
                                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                                        Children =
                                        {
                                            new Label {Text = Loc.GetString("Pressure:")},
                                            new Control {CustomMinimumSize = (4, 0)},
                                            (_pressureBar = new ProgressBar
                                            {
                                                CustomMinimumSize = (100, 20),
                                                SizeFlagsHorizontal = SizeFlags.FillExpand,
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
                                    new Control {CustomMinimumSize = (0, 10)},
                                    new HBoxContainer
                                    {
                                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                                        Children =
                                        {
                                            new Label {Text = Loc.GetString("Handle:")},
                                            new Control {
                                                CustomMinimumSize = (4, 0),
                                                SizeFlagsHorizontal = SizeFlags.FillExpand
                                            },
                                            (Engage = new Button
                                            {
                                                CustomMinimumSize = (16, 0),
                                                Text = Loc.GetString("Engage"),
                                                ToggleMode = true,
                                                Disabled = true
                                            })
                                        }
                                    },
                                    new Control {CustomMinimumSize = (0, 10)},
                                    new HBoxContainer
                                    {
                                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                                        Children =
                                        {
                                            new Label {Text = Loc.GetString("Eject:")},
                                            new Control {
                                                CustomMinimumSize = (4, 0),
                                                SizeFlagsHorizontal = SizeFlags.FillExpand
                                            },
                                            (Eject = new Button {
                                                CustomMinimumSize = (16, 0),
                                                Text = Loc.GetString("Eject Contents"),
                                                //SizeFlagsHorizontal = SizeFlags.ShrinkEnd
                                            })
                                        }
                                    },
                                    new Control {CustomMinimumSize = (0, 10)},
                                    new HBoxContainer
                                    {
                                        Children =
                                        {
                                            (Power = new CheckButton {Text = Loc.GetString("Power")}),
                                        }
                                    }
                                }
                            },
                        }
                    },
                    new MarginContainer
                    {
                        MarginLeftOverride = 12,
                        MarginRightOverride = 8,
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        Children =
                        {
                            new VBoxContainer
                            {
                                SizeFlagsHorizontal = SizeFlags.Fill,
                                Children =
                                {
                                    new HBoxContainer
                                    {
                                        Children =
                                        {
                                            new Label
                                            {
                                                Text = Loc.GetString("Select a destination:")
                                            }
                                        }
                                    },
                                    new Control { CustomMinimumSize = new Vector2(0, 8) },
                                    new HBoxContainer
                                    {
                                        SizeFlagsVertical = SizeFlags.FillExpand,
                                        Children =
                                        {
                                            (TargetListContainer = new ItemList
                                            {
                                                SelectMode = ItemList.ItemListSelectMode.Single,
                                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                                SizeFlagsVertical = SizeFlags.FillExpand
                                            })
                                        }
                                    },
                                    new PanelContainer
                                    {
                                        PanelOverride = new StyleBoxFlat
                                        {
                                            BackgroundColor = Color.FromHex("#ACBDBA")
                                        },
                                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                                        CustomMinimumSize = new Vector2(0, 1),
                                    },
                                    new HBoxContainer
                                    {
                                        Children =
                                        {
                                            new VBoxContainer
                                            {
                                                Children =
                                                {
                                                    new MarginContainer
                                                    {
                                                        MarginLeftOverride = 4,
                                                        Children =
                                                        {
                                                            new HBoxContainer
                                                            {
                                                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                                                Children =
                                                                {
                                                                    new Label
                                                                    {
                                                                        Text = Loc.GetString("This unit:")
                                                                    },
                                                                    new Control
                                                                    {
                                                                        CustomMinimumSize = new Vector2(4, 0)
                                                                    },
                                                                    (_tagLabel = new Label
                                                                    {
                                                                        Text = "-",
                                                                        SizeFlagsVertical = SizeFlags.ShrinkEnd
                                                                    })
                                                                }
                                                            }
                                                        }
                                                    },
                                                }
                                            }
                                        }
                                    }
                                }
                            }
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

        public void UpdateState(DisposalMailingUnitBoundUserInterfaceState state)
        {
            Title = state.UnitName;
            _unitState.Text = state.UnitState;
            UpdatePressureBar(state.Pressure);
            Power.Pressed = state.Powered;
            Engage.Pressed = state.Engaged;
            PopulateTargetList(state.Tags);
            _tagLabel.Text = state.Tag;
            TargetList = state.Tags;
        }

        private void PopulateTargetList(List<string> tags)
        {
            TargetListContainer.Clear();
            foreach (var target in tags)
            {
                TargetListContainer.AddItem(target);
            }
        }
    }
}
