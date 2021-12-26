using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared.Temperature;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.Atmos.Components.SharedGasAnalyzerComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Atmos.UI
{
    public class GasAnalyzerWindow : BaseWindow
    {
        public GasAnalyzerBoundUserInterface Owner { get; }

        private readonly Control _topContainer;
        private readonly Control _statusContainer;

        private readonly Label _nameLabel;

        public TextureButton CloseButton { get; set; }

        public GasAnalyzerWindow(GasAnalyzerBoundUserInterface owner)
        {
            var resourceCache = IoCManager.Resolve<IResourceCache>();

            Owner = owner;
            var rootContainer = new LayoutContainer { Name = "WireRoot" };
            AddChild(rootContainer);

            MouseFilter = MouseFilterMode.Stop;

            var panelTex = resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
            var back = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = Color.FromHex("#25252A"),
            };
            back.SetPatchMargin(StyleBox.Margin.All, 10);

            var topPanel = new PanelContainer
            {
                PanelOverride = back,
                MouseFilter = MouseFilterMode.Pass
            };
            var bottomWrap = new LayoutContainer
            {
                Name = "BottomWrap"
            };

            rootContainer.AddChild(topPanel);
            rootContainer.AddChild(bottomWrap);

            LayoutContainer.SetAnchorPreset(topPanel, LayoutContainer.LayoutPreset.Wide);
            LayoutContainer.SetMarginBottom(topPanel, -80);

            LayoutContainer.SetAnchorPreset(bottomWrap, LayoutContainer.LayoutPreset.VerticalCenterWide);
            LayoutContainer.SetGrowHorizontal(bottomWrap, LayoutContainer.GrowDirection.Both);

            var topContainerWrap = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    (_topContainer = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical
                    }),
                    new Control {MinSize = (0, 110)}
                }
            };

            rootContainer.AddChild(topContainerWrap);

            LayoutContainer.SetAnchorPreset(topContainerWrap, LayoutContainer.LayoutPreset.Wide);

            var font = resourceCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 13);
            var fontSmall = resourceCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 10);

            Button refreshButton;
            var topRow = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Margin = new Thickness(4, 4, 12, 2),
                Children =
                {
                    (_nameLabel = new Label
                    {
                        Text = Loc.GetString("gas-analyzer-window-name"),
                        FontOverride = font,
                        FontColorOverride = StyleNano.NanoGold,
                        VerticalAlignment = VAlignment.Center
                    }),
                    new Control
                    {
                        MinSize = (20, 0),
                        HorizontalExpand = true,
                    },
                    (refreshButton = new Button {Text = Loc.GetString("gas-analyzer-window-refresh-button")}), //TODO: refresh icon?
                    new Control
                    {
                        MinSize = (2, 0),
                    },
                    (CloseButton = new TextureButton
                    {
                        StyleClasses = {SS14Window.StyleClassWindowCloseButton},
                        VerticalAlignment = VAlignment.Center
                    })
                }
            };

            refreshButton.OnPressed += a =>
            {
                Owner.Refresh();
            };

            var middle = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#202025") },
                Children =
                {
                    (_statusContainer = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Margin = new Thickness(8, 8, 4, 4)
                    })
                }
            };

            _topContainer.AddChild(topRow);
            _topContainer.AddChild(new PanelContainer
            {
                MinSize = (0, 2),
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#525252ff") }
            });
            _topContainer.AddChild(middle);
            _topContainer.AddChild(new PanelContainer
            {
                MinSize = (0, 2),
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#525252ff") }
            });
            CloseButton.OnPressed += _ => Close();
            SetSize = (300, 200);
        }


        public void Populate(GasAnalyzerBoundUserInterfaceState state)
        {
            _statusContainer.RemoveAllChildren();
            if (state.Error != null)
            {
                _statusContainer.AddChild(new Label
                {
                    Text = Loc.GetString("gas-analyzer-window-error-text", ("errorText", state.Error)),
                    FontColorOverride = Color.Red
                });
                return;
            }

            _statusContainer.AddChild(new Label
            {
                Text = Loc.GetString("gas-analyzer-window-pressure-text", ("pressure", $"{state.Pressure:0.##}"))
            });
            _statusContainer.AddChild(new Label
            {
                Text = Loc.GetString("gas-analyzer-window-temperature-text",
                                     ("tempK", $"{state.Temperature:0.#}"),
                                     ("tempC", $"{TemperatureHelpers.KelvinToCelsius(state.Temperature):0.#}"))
            });
            // Return here cause all that stuff down there is gas stuff (so we don't get the seperators)
            if (state.Gases == null || state.Gases.Length == 0)
            {
                return;
            }

            // Seperator
            _statusContainer.AddChild(new Control
            {
                MinSize = new Vector2(0, 10)
            });

            // Add a table with all the gases
            var tableKey = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            var tableVal = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            _statusContainer.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    tableKey,
                    new Control
                    {
                        MinSize = new Vector2(20, 0)
                    },
                    tableVal
                }
            });
            // This is the gas bar thingy
            var height = 30;
            var minSize = 24; // This basically allows gases which are too small, to be shown properly
            var gasBar = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                MinSize = new Vector2(0, height)
            };
            // Seperator
            _statusContainer.AddChild(new Control
            {
                MinSize = new Vector2(0, 10)
            });

            var totalGasAmount = 0f;
            foreach (var gas in state.Gases)
            {
                totalGasAmount += gas.Amount;
            }

            for (int i = 0; i < state.Gases.Length; i++)
            {
                var gas = state.Gases[i];
                var color = Color.FromHex($"#{gas.Color}", Color.White);
                // Add to the table
                tableKey.AddChild(new Label
                {
                    Text = Loc.GetString(gas.Name)
                });
                tableVal.AddChild(new Label
                {
                    Text = Loc.GetString("gas-analyzer-window-molality-text", ("mol", $"{gas.Amount:0.##}"))
                });

                // Add to the gas bar //TODO: highlight the currently hover one
                var left = (i == 0) ? 0f : 2f;
                var right = (i == state.Gases.Length - 1) ? 0f : 2f;
                gasBar.AddChild(new PanelContainer
                {
                    ToolTip = Loc.GetString("gas-analyzer-window-molality-percentage-text",
                                            ("gasName", gas.Name),
                                            ("amount", $"{gas.Amount:0.##}"),
                                            ("percentage", $"{(gas.Amount / totalGasAmount * 100):0.#}")),
                    HorizontalExpand = true,
                    SizeFlagsStretchRatio = gas.Amount,
                    MouseFilter = MouseFilterMode.Pass,
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = color,
                        PaddingLeft = left,
                        PaddingRight = right
                    },
                    MinSize = new Vector2(minSize, 0)
                });
            }

            _statusContainer.AddChild(gasBar);
        }

        protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            return DragMode.Move;
        }

        protected override bool HasPoint(Vector2 point)
        {
            // This makes it so our base window won't count for hit tests,
            // but we will still receive mouse events coming in from Pass mouse filter mode.
            // So basically, it perfectly shells out the hit tests to the panels we have!
            return false;
        }
    }
}
