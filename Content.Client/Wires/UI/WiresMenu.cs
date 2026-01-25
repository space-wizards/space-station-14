using System.Numerics;
using Content.Client.Examine;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared.Wires;
using Robust.Client.Animations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Animations;
using Robust.Shared.Input;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Wires.UI
{
    public sealed class WiresMenu : BaseWindow
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private readonly Control _wiresHBox;
        private readonly Control _topContainer;
        private readonly Control _statusContainer;

        private readonly Label _nameLabel;
        private readonly Label _serialLabel;

        public TextureButton CloseButton { get; set; }

        public event Action<int, WiresAction>? OnAction;

        public WiresMenu()
        {
            IoCManager.InjectDependencies(this);

            var rootContainer = new LayoutContainer {Name = "WireRoot"};
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
            var bottomPanel = new PanelContainer
            {
                PanelOverride = back,
                MouseFilter = MouseFilterMode.Pass
            };

            var shadow = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    new PanelContainer
                    {
                        MinSize = new Vector2(2, 0),
                        PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#525252ff")}
                    },
                    new PanelContainer
                    {
                        HorizontalExpand = true,
                        MouseFilter = MouseFilterMode.Stop,
                        Name = "Shadow",
                        PanelOverride = new StyleBoxFlat {BackgroundColor = Color.Black.WithAlpha(0.5f)}
                    },
                    new PanelContainer
                    {
                        MinSize = new Vector2(2, 0),
                        PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#525252ff")}
                    },
                }
            };

            var wrappingHBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            _wiresHBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 4,
                VerticalAlignment = VAlignment.Bottom
            };

            wrappingHBox.AddChild(new Control {MinSize = new Vector2(20, 0)});
            wrappingHBox.AddChild(_wiresHBox);
            wrappingHBox.AddChild(new Control {MinSize = new Vector2(20, 0)});

            bottomWrap.AddChild(bottomPanel);

            LayoutContainer.SetAnchorPreset(bottomPanel, LayoutContainer.LayoutPreset.BottomWide);
            LayoutContainer.SetMarginTop(bottomPanel, -55);

            bottomWrap.AddChild(shadow);

            LayoutContainer.SetAnchorPreset(shadow, LayoutContainer.LayoutPreset.BottomWide);
            LayoutContainer.SetMarginBottom(shadow, -55);
            LayoutContainer.SetMarginTop(shadow, -80);
            LayoutContainer.SetMarginLeft(shadow, 12);
            LayoutContainer.SetMarginRight(shadow, -12);

            bottomWrap.AddChild(wrappingHBox);
            LayoutContainer.SetAnchorPreset(wrappingHBox, LayoutContainer.LayoutPreset.Wide);
            LayoutContainer.SetMarginBottom(wrappingHBox, -4);

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
                    new Control {MinSize = new Vector2(0, 110)}
                }
            };

            rootContainer.AddChild(topContainerWrap);

            LayoutContainer.SetAnchorPreset(topContainerWrap, LayoutContainer.LayoutPreset.Wide);

            var font = _resourceCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 13);
            var fontSmall = _resourceCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 10);

            Button helpButton;
            var topRow = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Margin = new Thickness(4, 2, 12, 2),
                Children =
                {
                    (_nameLabel = new Label
                    {
                        Text = Loc.GetString("wires-menu-name-label"),
                        FontOverride = font,
                        VerticalAlignment = VAlignment.Center,
                        StyleClasses = { StyleClass.LabelKeyText },
                    }),
                    (_serialLabel = new Label
                    {
                        Text = Loc.GetString("wires-menu-dead-beef-text"),
                        FontOverride = fontSmall,
                        FontColorOverride = Color.Gray,
                        VerticalAlignment = VAlignment.Center,
                        Margin = new Thickness(8, 0, 20, 0),
                        HorizontalAlignment = HAlignment.Left,
                        HorizontalExpand = true,
                    }),
                    (helpButton = new Button
                    {
                        Text = "?",
                        Margin = new Thickness(0, 0, 2, 0),
                    }),
                    (CloseButton = new TextureButton
                    {
                        StyleClasses = {DefaultWindow.StyleClassWindowCloseButton},
                        VerticalAlignment = VAlignment.Center
                    })
                }
            };

            helpButton.OnPressed += a =>
            {
                var popup = new HelpPopup();
                UserInterfaceManager.ModalRoot.AddChild(popup);

                popup.Open(UIBox2.FromDimensions(a.Event.PointerLocation.Position, new Vector2(400, 200)));
            };

            var middle = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#202025")},
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            (_statusContainer = new GridContainer
                            {
                                Margin = new Thickness(8, 4),
                                Rows = 2
                            })
                        }
                    }
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
            CloseButton.OnPressed += _ => Close();
            SetHeight = 200;
            MinWidth = 320;
        }


        public void Populate(WiresBoundUserInterfaceState state)
        {
            _nameLabel.Text = state.BoardName;
            _serialLabel.Text = state.SerialNumber;

            _wiresHBox.RemoveAllChildren();
            var random = new Random(state.WireSeed);
            foreach (var wire in state.WiresList)
            {
                var mirror = random.Next(2) == 0;
                var flip = random.Next(2) == 0;
                var type = random.Next(2);
                var control = new WireControl(wire.Color, wire.Letter, wire.IsCut, flip, mirror, type, _resourceCache)
                {
                    VerticalAlignment = VAlignment.Bottom
                };
                _wiresHBox.AddChild(control);

                control.WireClicked += () =>
                {
                    OnAction?.Invoke(wire.Id, wire.IsCut ? WiresAction.Mend : WiresAction.Cut);
                };

                control.ContactsClicked += () =>
                {
                    OnAction?.Invoke(wire.Id, WiresAction.Pulse);
                };
            }

            _statusContainer.RemoveAllChildren();

            foreach (var status in state.Statuses)
            {
                if (status.Value is StatusLightData statusLightData)
                {
                    _statusContainer.AddChild(new StatusLight(statusLightData, _resourceCache));
                }
                else
                {
                    _statusContainer.AddChild(new Label
                    {
                        Text = status.ToString()
                    });
                }
            }
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

        private sealed class WireControl : Control
        {
            private IResourceCache _resourceCache;

            private const string TextureContact = "/Textures/Interface/WireHacking/contact.svg.96dpi.png";

            public event Action? WireClicked;
            public event Action? ContactsClicked;

            public WireControl(WireColor color, WireLetter letter, bool isCut, bool flip, bool mirror, int type,
                IResourceCache resourceCache)
            {
                _resourceCache = resourceCache;

                HorizontalAlignment = HAlignment.Center;
                MouseFilter = MouseFilterMode.Stop;

                var layout = new LayoutContainer();
                AddChild(layout);

                var greek = new Label
                {
                    Text = letter.Letter().ToString(),
                    VerticalAlignment = VAlignment.Bottom,
                    HorizontalAlignment = HAlignment.Center,
                    Align = Label.AlignMode.Center,
                    FontOverride = _resourceCache.GetFont("/Fonts/NotoSansDisplay/NotoSansDisplay-Bold.ttf", 12),
                    FontColorOverride = Color.Gray,
                    ToolTip = letter.Name(),
                    MouseFilter = MouseFilterMode.Stop
                };

                layout.AddChild(greek);
                LayoutContainer.SetAnchorPreset(greek, LayoutContainer.LayoutPreset.BottomWide);
                LayoutContainer.SetGrowVertical(greek, LayoutContainer.GrowDirection.Begin);
                LayoutContainer.SetGrowHorizontal(greek, LayoutContainer.GrowDirection.Both);

                var contactTexture = _resourceCache.GetTexture(TextureContact);
                var contact1 = new TextureRect
                {
                    Texture = contactTexture,
                    Modulate = Color.FromHex("#E1CA76")
                };

                layout.AddChild(contact1);
                LayoutContainer.SetPosition(contact1, new Vector2(0, 0));

                var contact2 = new TextureRect
                {
                    Texture = contactTexture,
                    Modulate = Color.FromHex("#E1CA76")
                };

                layout.AddChild(contact2);
                LayoutContainer.SetPosition(contact2, new Vector2(0, 60));

                var wire = new WireRender(color, isCut, flip, mirror, type, _resourceCache);

                layout.AddChild(wire);
                LayoutContainer.SetPosition(wire, new Vector2(2, 16));

                ToolTip = color.Name();
                MinSize = new Vector2(20, 102);
            }

            protected override void KeyBindDown(GUIBoundKeyEventArgs args)
            {
                base.KeyBindDown(args);

                if (args.Function != EngineKeyFunctions.UIClick)
                {
                    return;
                }

                if (args.RelativePosition.Y > 20 && args.RelativePosition.Y < 60)
                {
                    WireClicked?.Invoke();
                }
                else
                {
                    ContactsClicked?.Invoke();
                }
            }

            protected override bool HasPoint(Vector2 point)
            {
                return base.HasPoint(point) && point.Y <= 80;
            }

            private sealed class WireRender : Control
            {
                private readonly WireColor _color;
                private readonly bool _isCut;
                private readonly bool _flip;
                private readonly bool _mirror;
                private readonly int _type;

                private static readonly string[] TextureNormal =
                {
                    "/Textures/Interface/WireHacking/wire_1.svg.96dpi.png",
                    "/Textures/Interface/WireHacking/wire_2.svg.96dpi.png"
                };

                private static readonly string[] TextureCut =
                {
                    "/Textures/Interface/WireHacking/wire_1_cut.svg.96dpi.png",
                    "/Textures/Interface/WireHacking/wire_2_cut.svg.96dpi.png",
                };

                private static readonly string[] TextureCopper =
                {
                    "/Textures/Interface/WireHacking/wire_1_copper.svg.96dpi.png",
                    "/Textures/Interface/WireHacking/wire_2_copper.svg.96dpi.png"
                };

                private readonly IResourceCache _resourceCache;

                public WireRender(WireColor color, bool isCut, bool flip, bool mirror, int type,
                    IResourceCache resourceCache)
                {
                    _resourceCache = resourceCache;
                    _color = color;
                    _isCut = isCut;
                    _flip = flip;
                    _mirror = mirror;
                    _type = type;

                    SetSize = new Vector2(16, 50);
                }

                protected override void Draw(DrawingHandleScreen handle)
                {
                    var colorValue = _color.ColorValue();
                    var tex = _resourceCache.GetTexture(_isCut ? TextureCut[_type] : TextureNormal[_type]);

                    var l = 0f;
                    var r = tex.Width + l;
                    var t = 0f;
                    var b = tex.Height + t;

                    if (_flip)
                    {
                        (t, b) = (b, t);
                    }

                    if (_mirror)
                    {
                        (l, r) = (r, l);
                    }

                    l *= UIScale;
                    r *= UIScale;
                    t *= UIScale;
                    b *= UIScale;

                    var rect = new UIBox2(l, t, r, b);
                    if (_isCut)
                    {
                        var copper = Color.Orange;
                        var copperTex = _resourceCache.GetTexture(TextureCopper[_type]);
                        handle.DrawTextureRect(copperTex, rect, copper);
                    }

                    handle.DrawTextureRect(tex, rect, colorValue);
                }
            }
        }

        private sealed class StatusLight : Control
        {
            private static readonly Animation _blinkingFast = new()
            {
                Length = TimeSpan.FromSeconds(0.2),
                AnimationTracks =
                {
                    new AnimationTrackControlProperty
                    {
                        Property = nameof(Control.Modulate),
                        InterpolationMode = AnimationInterpolationMode.Linear,
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(Color.White, 0f),
                            new AnimationTrackProperty.KeyFrame(Color.Transparent, 0.1f),
                            new AnimationTrackProperty.KeyFrame(Color.White, 0.1f)
                        }
                    }
                }
            };

            private static readonly Animation _blinkingSlow = new()
            {
                Length = TimeSpan.FromSeconds(0.8),
                AnimationTracks =
                {
                    new AnimationTrackControlProperty
                    {
                        Property = nameof(Control.Modulate),
                        InterpolationMode = AnimationInterpolationMode.Linear,
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(Color.White, 0f),
                            new AnimationTrackProperty.KeyFrame(Color.White, 0.3f),
                            new AnimationTrackProperty.KeyFrame(Color.Transparent, 0.1f),
                            new AnimationTrackProperty.KeyFrame(Color.Transparent, 0.3f),
                            new AnimationTrackProperty.KeyFrame(Color.White, 0.1f),
                        }
                    }
                }
            };

            public StatusLight(StatusLightData data, IResourceCache resourceCache)
            {
                HorizontalAlignment = HAlignment.Right;

                var hsv = Color.ToHsv(data.Color);
                hsv.Z /= 2;
                var dimColor = Color.FromHsv(hsv);
                TextureRect activeLight;

                var lightContainer = new Control
                {
                    SetSize = new Vector2(20, 20),
                    Children =
                    {
                        new TextureRect
                        {
                            Texture = resourceCache.GetTexture(
                                "/Textures/Interface/WireHacking/light_off_base.svg.96dpi.png"),
                            Stretch = TextureRect.StretchMode.KeepCentered,
                            ModulateSelfOverride = dimColor
                        },
                        (activeLight = new TextureRect
                        {
                            ModulateSelfOverride = data.Color.WithAlpha(0.4f),
                            Stretch = TextureRect.StretchMode.KeepCentered,
                            Texture =
                                resourceCache.GetTexture("/Textures/Interface/WireHacking/light_on_base.svg.96dpi.png"),
                        })
                    }
                };

                Animation? animation = null;

                switch (data.State)
                {
                    case StatusLightState.Off:
                        activeLight.Visible = false;
                        break;
                    case StatusLightState.On:
                        break;
                    case StatusLightState.BlinkingFast:
                        animation = _blinkingFast;
                        break;
                    case StatusLightState.BlinkingSlow:
                        animation = _blinkingSlow;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (animation != null)
                {
                    activeLight.PlayAnimation(animation, "blink");

                    activeLight.AnimationCompleted += s =>
                    {
                        if (s == "blink")
                        {
                            activeLight.PlayAnimation(animation, s);
                        }
                    };
                }

                var font = resourceCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 12);

                var hBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    SeparationOverride = 4
                };
                hBox.AddChild(new Label
                {
                    Text = data.Text,
                    FontOverride = font,
                    FontColorOverride = Color.FromHex("#A1A6AE"),
                    VerticalAlignment = VAlignment.Center,
                });
                hBox.AddChild(lightContainer);
                hBox.AddChild(new Control {MinSize = new Vector2(6, 0)});
                AddChild(hBox);
            }
        }

        private sealed class HelpPopup : Popup
        {
            public HelpPopup()
            {
                var label = new RichTextLabel();
                label.SetMessage(Loc.GetString("wires-menu-help-popup"));
                AddChild(new PanelContainer
                {
                    StyleClasses = {ExamineSystem.StyleClassEntityTooltip},
                    Children = {label}
                });
            }
        }
    }
}
