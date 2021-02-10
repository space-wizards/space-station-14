using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Atmos.GasTank;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Atmos.GasTank
{
    public class GasTankWindow
        : BaseWindow
    {
        private GasTankBoundUserInterface _owner;
        private readonly Label _lblName;
        private readonly VBoxContainer _topContainer;
        private readonly Control _contentContainer;


        private readonly IResourceCache _resourceCache = default!;
        private readonly RichTextLabel _lblPressure;
        private readonly FloatSpinBox _spbPressure;
        private readonly RichTextLabel _lblInternals;
        private readonly Button _btnInternals;

        public GasTankWindow(GasTankBoundUserInterface owner)
        {
            TextureButton btnClose;
            _resourceCache = IoCManager.Resolve<IResourceCache>();
            _owner = owner;
            var rootContainer = new LayoutContainer {Name = "GasTankRoot"};
            AddChild(rootContainer);

            MouseFilter = MouseFilterMode.Stop;

            var panelTex = _resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
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
            LayoutContainer.SetMarginBottom(topPanel, -85);

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

            var font = _resourceCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 13);

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
                            (_lblName = new Label
                            {
                                Text = Loc.GetString("Gas Tank"),
                                FontOverride = font,
                                FontColorOverride = StyleNano.NanoGold,
                                SizeFlagsVertical = SizeFlags.ShrinkCenter
                            }),
                            new Control
                            {
                                CustomMinimumSize = (20, 0),
                                SizeFlagsHorizontal = SizeFlags.Expand
                            },
                            (btnClose = new TextureButton
                            {
                                StyleClasses = {SS14Window.StyleClassWindowCloseButton},
                                SizeFlagsVertical = SizeFlags.ShrinkCenter
                            })
                        }
                    }
                }
            };

            var middle = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#202025")},
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
                            (_contentContainer = new VBoxContainer())
                        }
                    }
                }
            };

            _topContainer.AddChild(topRow);
            _topContainer.AddChild(new PanelContainer
            {
                CustomMinimumSize = (0, 2),
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#525252ff")}
            });
            _topContainer.AddChild(middle);
            _topContainer.AddChild(new PanelContainer
            {
                CustomMinimumSize = (0, 2),
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#525252ff")}
            });


            _lblPressure = new RichTextLabel();
            _contentContainer.AddChild(_lblPressure);

            //internals
            _lblInternals = new RichTextLabel
                {CustomMinimumSize = (200, 0), SizeFlagsVertical = SizeFlags.ShrinkCenter};
            _btnInternals = new Button {Text = Loc.GetString("Toggle")};

            _contentContainer.AddChild(
                new MarginContainer
                {
                    MarginTopOverride = 7,
                    Children =
                    {
                        new HBoxContainer
                        {
                            Children = {_lblInternals, _btnInternals}
                        }
                    }
                });

            // Separator
            _contentContainer.AddChild(new Control
            {
                CustomMinimumSize = new Vector2(0, 10)
            });

            _contentContainer.AddChild(new Label
            {
                Text = Loc.GetString("Output Pressure"),
                Align = Label.AlignMode.Center
            });
            _spbPressure = new FloatSpinBox {IsValid = f => f >= 0 || f <= 3000};
            _contentContainer.AddChild(
                new MarginContainer
                {
                    MarginRightOverride = 25,
                    MarginLeftOverride = 25,
                    MarginBottomOverride = 7,
                    Children =
                    {
                        _spbPressure
                    }
                }
            );

            // Handlers
            _spbPressure.OnValueChanged += args =>
            {
                _owner.SetOutputPressure(args.Value);
            };

            _btnInternals.OnPressed += args =>
            {
                _owner.ToggleInternals();
            };

            btnClose.OnPressed += _ => Close();
        }

        public void UpdateState(GasTankBoundUserInterfaceState state)
        {
            _lblPressure.SetMarkup(Loc.GetString("Pressure: {0:0.##} kPa", state.TankPressure));
            _btnInternals.Disabled = !state.CanConnectInternals;
            _lblInternals.SetMarkup(Loc.GetString("Internals: [color={0}]{1}[/color]",
                state.InternalsConnected ? "green" : "red",
                state.InternalsConnected ? "Connected" : "Disconnected"));
            if (state.OutputPressure.HasValue)
            {
                _spbPressure.Value = state.OutputPressure.Value;
            }
        }

        protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            return DragMode.Move;
        }

        protected override bool HasPoint(Vector2 point)
        {
            return false;
        }
    }
}
