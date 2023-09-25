using System.Numerics;
using Content.Client.Message;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared.Atmos.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.UserInterface.Systems.Atmos.GasTank
{
    public sealed class GasTankWindow
        : BaseWindow
    {
        private GasTankBoundUserInterface _owner;
        private readonly Label _lblName;
        private readonly BoxContainer _topContainer;
        private readonly Control _contentContainer;


        private readonly IClientResourceCache _resourceCache = default!;
        private readonly RichTextLabel _lblPressure;
        private readonly FloatSpinBox _spbPressure;
        private readonly RichTextLabel _lblInternals;
        private readonly Button _btnInternals;

        public GasTankWindow(GasTankBoundUserInterface owner)
        {
            TextureButton btnClose;
            _resourceCache = IoCManager.Resolve<IClientResourceCache>();
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


            var topContainerWrap = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    (_topContainer = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical
                    }),
                    new Control {MinSize = new Vector2(0, 110)}
                }
            };

            rootContainer.AddChild(topContainerWrap);

            LayoutContainer.SetAnchorPreset(topContainerWrap, LayoutContainer.LayoutPreset.Wide);

            var font = _resourceCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 13);

            var topRow = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Margin = new Thickness(4, 2, 12, 2),
                Children =
                {
                    (_lblName = new Label
                    {
                        Text = Loc.GetString("gas-tank-window-label"),
                        FontOverride = font,
                        FontColorOverride = StyleNano.NanoGold,
                        VerticalAlignment = VAlignment.Center,
                        HorizontalExpand = true,
                        HorizontalAlignment = HAlignment.Left,
                        Margin = new Thickness(0, 0, 20, 0),
                    }),
                    (btnClose = new TextureButton
                    {
                        StyleClasses = {DefaultWindow.StyleClassWindowCloseButton},
                        VerticalAlignment = VAlignment.Center
                    })
                }
            };

            var middle = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#202025")},
                Children =
                {
                    (_contentContainer = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Margin = new Thickness(8, 4),
                    })
                }
            };

            _topContainer.AddChild(topRow);
            _topContainer.AddChild(new PanelContainer
            {
                MinSize = new Vector2(0, 2),
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#525252ff")}
            });
            _topContainer.AddChild(middle);
            _topContainer.AddChild(new PanelContainer
            {
                MinSize = new Vector2(0, 2),
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#525252ff")}
            });


            _lblPressure = new RichTextLabel();
            _contentContainer.AddChild(_lblPressure);

            //internals
            _lblInternals = new RichTextLabel
                {MinSize = new Vector2(200, 0), VerticalAlignment = VAlignment.Center};
            _btnInternals = new Button {Text = Loc.GetString("gas-tank-window-internals-toggle-button") };

            _contentContainer.AddChild(
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Margin = new Thickness(0, 7, 0, 0),
                    Children = {_lblInternals, _btnInternals}
                });

            // Separator
            _contentContainer.AddChild(new Control
            {
                MinSize = new Vector2(0, 10)
            });

            _contentContainer.AddChild(new Label
            {
                Text = Loc.GetString("gas-tank-window-output-pressure-label"),
                Align = Label.AlignMode.Center
            });
            _spbPressure = new FloatSpinBox
            {
                IsValid = f => f >= 0 || f <= 3000,
                Margin = new Thickness(25, 0, 25, 7)
            };
            _contentContainer.AddChild(_spbPressure);

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
            _lblPressure.SetMarkup(Loc.GetString("gas-tank-window-tank-pressure-text", ("tankPressure", $"{state.TankPressure:0.##}")));
            _btnInternals.Disabled = !state.CanConnectInternals;
            _lblInternals.SetMarkup(Loc.GetString("gas-tank-window-internal-text",
                ("status", Loc.GetString(state.InternalsConnected ? "gas-tank-window-internal-connected" : "gas-tank-window-internal-disconnected"))));
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
