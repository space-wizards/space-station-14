using System;
using Content.Client.Animations;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Client.Animations;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Animations;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.SharedGasAnalyzerComponent;

namespace Content.Client.GameObjects.Components.Atmos
{
    public class GasAnalyzerWindow : BaseWindow
    {
        public GasAnalyzerBoundUserInterface Owner { get; }

        private readonly Control _wiresHBox;
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

            var topContainerWrap = new VBoxContainer
            {
                Children =
                {
                    (_topContainer = new VBoxContainer()),
                    new Control {CustomMinimumSize = (0, 110)}
                }
            };

            rootContainer.AddChild(topContainerWrap);

            LayoutContainer.SetAnchorPreset(topContainerWrap, LayoutContainer.LayoutPreset.Wide);

            var font = resourceCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 13);
            var fontSmall = resourceCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 10);

            Button refreshButton;
            var topRow = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 2,
                MarginRightOverride = 12,
                MarginBottomOverride = 2,
                Children =
                {
                    new HBoxContainer
                    {
                        Children =
                        {
                            (_nameLabel = new Label
                            {
                                Text = Loc.GetString("Gas Analyzer"),
                                FontOverride = font,
                                FontColorOverride = StyleNano.NanoGold,
                                SizeFlagsVertical = SizeFlags.ShrinkCenter
                            }),
                            new Control
                            {
                                CustomMinimumSize = (20, 0),
                                SizeFlagsHorizontal = SizeFlags.Expand
                            },
                            (refreshButton = new Button {Text = "Refresh"}), //TODO: refresh icon?
                            new Control
                            {
                                CustomMinimumSize = (2, 0),
                            },
                            (CloseButton = new TextureButton
                            {
                                StyleClasses = {SS14Window.StyleClassWindowCloseButton},
                                SizeFlagsVertical = SizeFlags.ShrinkCenter
                            })
                        }
                    }
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
                    new HBoxContainer
                    {
                        Children =
                        {
                            new MarginContainer
                            {
                                MarginLeftOverride = 8,
                                MarginRightOverride = 8,
                                MarginTopOverride = 4,
                                MarginBottomOverride = 4,
                                Children =
                                {
                                    (_statusContainer = new VBoxContainer())
                                }
                            }
                        }
                    }
                }
            };

            _topContainer.AddChild(topRow);
            _topContainer.AddChild(new PanelContainer
            {
                CustomMinimumSize = (0, 2),
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#525252ff") }
            });
            _topContainer.AddChild(middle);
            _topContainer.AddChild(new PanelContainer
            {
                CustomMinimumSize = (0, 2),
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#525252ff") }
            });
            CloseButton.OnPressed += _ => Close();
            LayoutContainer.SetSize(this, (300, 200));
        }


        public void Populate(GasAnalyzerBoundUserInterfaceState state)
        {
            _statusContainer.RemoveAllChildren();
            if (state.Error != null)
            {
                _statusContainer.AddChild(new Label
                {
                    Text = Loc.GetString("Error: {0}", state.Error),
                    FontColorOverride = Color.Red
                });
                return;
            }
            
            _statusContainer.AddChild(new Label
            {
                Text = Loc.GetString("Pressure: {0:0.##}", state.Pressure)
            });
            _statusContainer.AddChild(new Label
            {
                Text = Loc.GetString("Temperature: {0:0.#}K ({1:0.#}°C)", state.Pressure, TemperatureHelpers.KelvinToCelsius(state.Pressure))
            });
            foreach (var gas in state.Gases)
            {
                _statusContainer.AddChild(new Label
                {
                    Text = gas.ToString()
                });
            }
            /*_nameLabel.Text = state.BoardName;
            _serialLabel.Text = state.SerialNumber;

            _wiresHBox.RemoveAllChildren();
            var random = new Random(state.WireSeed);
            foreach (var wire in state.WiresList)
            {
                var mirror = random.Next(2) == 0;
                var flip = random.Next(2) == 0;
                var type = random.Next(2);
                var control = new WireControl(wire.Color, wire.Letter, wire.IsCut, flip, mirror, type)
                {
                    SizeFlagsVertical = SizeFlags.ShrinkEnd
                };
                _wiresHBox.AddChild(control);

                control.WireClicked += () =>
                {
                    Owner.PerformAction(wire.Id, wire.IsCut ? WiresAction.Mend : WiresAction.Cut);
                };

                control.ContactsClicked += () =>
                {
                    Owner.PerformAction(wire.Id, WiresAction.Pulse);
                };
            }


            _statusContainer.RemoveAllChildren();
            foreach (var status in state.Statuses)
            {
                if (status.Value is StatusLightData statusLightData)
                {
                    _statusContainer.AddChild(new StatusLight(statusLightData));
                }
                else
                {
                    _statusContainer.AddChild(new Label
                    {
                        Text = status.ToString()
                    });
                }
            }*/
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
