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
using static Content.Shared.Medical.Components.SharedHealthAnalyzerComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Medical.UI
{
    public class HealthAnalyzerWindow : BaseWindow
    {
        public HealthAnalyzerBoundUserInterface Owner { get; }

        private readonly Control _topContainer;
        private readonly Control _statusContainer;

        private readonly Label _nameLabel;

        public TextureButton CloseButton { get; set; }

        public HealthAnalyzerWindow(HealthAnalyzerBoundUserInterface owner)
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
                        Text = Loc.GetString("health-analyzer-window-name"),
                        FontOverride = font,
                        FontColorOverride = StyleNano.NanoGold,
                        VerticalAlignment = VAlignment.Center
                    }),
                    new Control
                    {
                        MinSize = (20, 0),
                        HorizontalExpand = true,
                    },
                    (refreshButton = new Button {Text = Loc.GetString("health-analyzer-window-refresh-button")}), //TODO: refresh icon?
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


        public void Populate(HealthAnalyzerBoundUserInterfaceState state)
        {
            _statusContainer.RemoveAllChildren();
            if (state.Error != null)
            {
                _statusContainer.AddChild(new Label
                {
                    Text = Loc.GetString("health-analyzer-window-error-text", ("errorText", state.Error)),
                    FontColorOverride = Color.Red
                });
                return;
            }

            _statusContainer.AddChild(new Label
            {
                Text = Loc.GetString("health-analyzer-overall-status", ("health", $"{state.Health:0.##}"))
            });

            // Seperator
            _statusContainer.AddChild(new Control
            {
                MinSize = new Vector2(0, 10)
            });

            //TODO: add some crude organ visualisation with tooltip info overlay
            //See gas analyzer#gases, use proper collection of body parts
            //Blood level, etc, etc
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
