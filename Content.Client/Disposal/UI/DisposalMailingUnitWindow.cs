using System.Collections.Generic;
using Content.Shared.Disposal.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.Disposal.Components.SharedDisposalMailingUnitComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Disposal.UI
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

        public DisposalMailingUnitWindow()
        {
            MinSize = SetSize = (460, 230);
            TargetList = new List<string>();
            Contents.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        HorizontalExpand = true,
                        Margin = new Thickness(8, 0),
                        Children =
                        {
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                Children =
                                {
                                    new Label {Text = $"{Loc.GetString("disposal-mailing-unit-window-state-label")} "},
                                    new Control {MinSize = (4, 0)},
                                    (_unitState = new Label {Text = Loc.GetString("disposal-mailing-unit-window-ready-state")})
                                }
                            },
                            new Control {MinSize = (0, 10)},
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                HorizontalExpand = true,
                                Children =
                                {
                                    new Label {Text = Loc.GetString("disposal-mailing-unit-pressure-label")},
                                    new Control {MinSize = (4, 0)},
                                    (_pressureBar = new ProgressBar
                                    {
                                        MinSize = (100, 20),
                                        HorizontalExpand = true,
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
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                HorizontalExpand = true,
                                Children =
                                {
                                    new Label {Text = Loc.GetString("disposal-mailing-unit-handle-label")},
                                    new Control
                                    {
                                        MinSize = (4, 0),
                                        HorizontalExpand = true
                                    },
                                    (Engage = new Button
                                    {
                                        MinSize = (16, 0),
                                        Text = Loc.GetString("disposal-mailing-unit-engage-button"),
                                        ToggleMode = true,
                                        Disabled = true
                                    })
                                }
                            },
                            new Control {MinSize = (0, 10)},
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                HorizontalExpand = true,
                                Children =
                                {
                                    new Label {Text = Loc.GetString("disposal-mailing-unit-eject-label")},
                                    new Control
                                    {
                                        MinSize = (4, 0),
                                        HorizontalExpand = true
                                    },
                                    (Eject = new Button
                                    {
                                        MinSize = (16, 0),
                                        Text = Loc.GetString("disposal-mailing-unit-eject-button"),
                                        //HorizontalAlignment = HAlignment.Right
                                    })
                                }
                            },
                            new Control {MinSize = (0, 10)},
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                Children =
                                {
                                    (Power = new CheckButton {Text = Loc.GetString("disposal-mailing-unit-power-button")}),
                                }
                            }
                        }
                    },
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Margin = new Thickness(12, 0, 8, 0),
                        Children =
                        {
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = Loc.GetString("disposal-mailing-unit-destination-select-label")
                                    }
                                }
                            },
                            new Control {MinSize = new Vector2(0, 8)},
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                VerticalExpand = true,
                                Children =
                                {
                                    (TargetListContainer = new ItemList
                                    {
                                        SelectMode = ItemList.ItemListSelectMode.Single,
                                        HorizontalExpand = true,
                                        VerticalExpand = true
                                    })
                                }
                            },
                            new PanelContainer
                            {
                                PanelOverride = new StyleBoxFlat
                                {
                                    BackgroundColor = Color.FromHex("#ACBDBA")
                                },
                                HorizontalExpand = true,
                                MinSize = new Vector2(0, 1),
                            },
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                Children =
                                {
                                    new BoxContainer
                                    {
                                        Orientation = LayoutOrientation.Vertical,
                                        Children =
                                        {
                                            new BoxContainer
                                            {
                                                Orientation = LayoutOrientation.Horizontal,
                                                Margin = new Thickness(4, 0, 0, 0),
                                                Children =
                                                {
                                                    new Label
                                                    {
                                                        Text = Loc.GetString("disposal-mailing-unit-unit-self-reference")
                                                    },
                                                    new Control
                                                    {
                                                        MinSize = new Vector2(4, 0)
                                                    },
                                                    (_tagLabel = new Label
                                                    {
                                                        Text = "-",
                                                        VerticalAlignment = VAlignment.Bottom
                                                    })
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
