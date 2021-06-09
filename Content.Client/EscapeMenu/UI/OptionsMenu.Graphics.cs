using Content.Shared;
using Content.Shared.Prototypes.HUD;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface
{
    public sealed partial class OptionsMenu
    {
        private sealed class GraphicsControl : Control
        {
            private static readonly float[] UIScaleOptions =
            {
                0f,
                0.75f,
                1f,
                1.25f,
                1.50f,
                1.75f,
                2f
            };

            private readonly IConfigurationManager _cfg;
            private readonly IPrototypeManager _prototypeManager;

            private readonly Button ApplyButton;
            private readonly CheckBox VSyncCheckBox;
            private readonly CheckBox FullscreenCheckBox;
            private readonly OptionButton LightingPresetOption;
            private readonly OptionButton _uiScaleOption;
            private readonly OptionButton _hudThemeOption;
            private readonly CheckBox _viewportStretchCheckBox;
            private readonly CheckBox _viewportLowResCheckBox;
            private readonly Slider _viewportScaleSlider;
            private readonly Control _viewportScaleBox;
            private readonly Label _viewportScaleText;

            public GraphicsControl(IConfigurationManager cfg, IPrototypeManager proMan)
            {
                _cfg = cfg;
                _prototypeManager = proMan;
                var vBox = new VBoxContainer();

                var contents = new VBoxContainer
                {
                    Margin = new Thickness(2, 2, 2, 0),
                    VerticalExpand = true,
                };

                VSyncCheckBox = new CheckBox {Text = Loc.GetString("ui-options-vsync")};
                contents.AddChild(VSyncCheckBox);
                VSyncCheckBox.OnToggled += OnCheckBoxToggled;

                FullscreenCheckBox = new CheckBox {Text = Loc.GetString("ui-options-fullscreen")};
                contents.AddChild(FullscreenCheckBox);
                FullscreenCheckBox.OnToggled += OnCheckBoxToggled;

                LightingPresetOption = new OptionButton {MinSize = (100, 0)};
                LightingPresetOption.AddItem(Loc.GetString("ui-options-lighting-very-low"));
                LightingPresetOption.AddItem(Loc.GetString("ui-options-lighting-low"));
                LightingPresetOption.AddItem(Loc.GetString("ui-options-lighting-medium"));
                LightingPresetOption.AddItem(Loc.GetString("ui-options-lighting-high"));
                LightingPresetOption.OnItemSelected += OnLightingQualityChanged;

                contents.AddChild(new HBoxContainer
                {
                    Children =
                    {
                        new Label {Text = Loc.GetString("ui-options-lighting-label")},
                        new Control {MinSize = (4, 0)},
                        LightingPresetOption
                    }
                });

                ApplyButton = new Button
                {
                    Text = Loc.GetString("ui-options-apply"), TextAlign = Label.AlignMode.Center,
                    HorizontalAlignment = HAlignment.Right
                };

                _uiScaleOption = new OptionButton();
                _uiScaleOption.AddItem(Loc.GetString("ui-options-scale-auto",
                    ("scale", UserInterfaceManager.DefaultUIScale)));
                _uiScaleOption.AddItem(Loc.GetString("ui-options-scale-75"));
                _uiScaleOption.AddItem(Loc.GetString("ui-options-scale-100"));
                _uiScaleOption.AddItem(Loc.GetString("ui-options-scale-125"));
                _uiScaleOption.AddItem(Loc.GetString("ui-options-scale-150"));
                _uiScaleOption.AddItem(Loc.GetString("ui-options-scale-175"));
                _uiScaleOption.AddItem(Loc.GetString("ui-options-scale-200"));
                _uiScaleOption.OnItemSelected += OnUIScaleChanged;

                contents.AddChild(new HBoxContainer
                {
                    Children =
                    {
                        new Label {Text = Loc.GetString("ui-options-scale-label")},
                        new Control {MinSize = (4, 0)},
                        _uiScaleOption
                    }
                });

                _hudThemeOption = new OptionButton();
                foreach (var gear in _prototypeManager.EnumeratePrototypes<HudThemePrototype>())
                {
                   _hudThemeOption.AddItem(Loc.GetString(gear.Name));
                }
                _hudThemeOption.OnItemSelected += OnHudThemeChanged;

                contents.AddChild(new HBoxContainer
                {
                    Children =
                    {
                        new Label {Text = Loc.GetString("ui-options-hud-theme")},
                        new Control {MinSize = (4, 0)},
                        _hudThemeOption
                    }
                });

                _viewportStretchCheckBox = new CheckBox
                {
                    Text = Loc.GetString("ui-options-vp-stretch")
                };

                _viewportStretchCheckBox.OnToggled += _ =>
                {
                    UpdateViewportScale();
                    UpdateApplyButton();
                };

                _viewportScaleSlider = new Slider
                {
                    MinValue = 1,
                    MaxValue = 5,
                    Rounded = true,
                    MinWidth = 200
                };

                _viewportScaleSlider.OnValueChanged += _ =>
                {
                    UpdateApplyButton();
                    UpdateViewportScale();
                };

                _viewportLowResCheckBox = new CheckBox { Text = Loc.GetString("ui-options-vp-low-res")};
                _viewportLowResCheckBox.OnToggled += OnCheckBoxToggled;

                contents.AddChild(new HBoxContainer
                {
                    Children =
                    {
                        _viewportStretchCheckBox,
                        (_viewportScaleBox = new HBoxContainer
                        {
                            Children =
                            {
                                (_viewportScaleText = new Label
                                {
                                    Margin = new Thickness(8, 0)
                                }),
                                _viewportScaleSlider,
                            }
                        })
                    }
                });

                contents.AddChild(_viewportLowResCheckBox);

                vBox.AddChild(contents);

                vBox.AddChild(new StripeBack
                {
                    HasBottomEdge = false,
                    HasMargins = false,
                    Children =
                    {
                        ApplyButton
                    }
                });
                ApplyButton.OnPressed += OnApplyButtonPressed;

                VSyncCheckBox.Pressed = _cfg.GetCVar(CVars.DisplayVSync);
                FullscreenCheckBox.Pressed = ConfigIsFullscreen;
                LightingPresetOption.SelectId(GetConfigLightingQuality());
                _uiScaleOption.SelectId(GetConfigUIScalePreset(ConfigUIScale));
                _hudThemeOption.SelectId(_cfg.GetCVar(CCVars.HudTheme));
                _viewportScaleSlider.Value = _cfg.GetCVar(CCVars.ViewportFixedScaleFactor);
                _viewportStretchCheckBox.Pressed = _cfg.GetCVar(CCVars.ViewportStretch);
                _viewportLowResCheckBox.Pressed = !_cfg.GetCVar(CCVars.ViewportScaleRender);

                UpdateViewportScale();
                UpdateApplyButton();

                AddChild(vBox);
            }

            private void OnUIScaleChanged(OptionButton.ItemSelectedEventArgs args)
            {
                _uiScaleOption.SelectId(args.Id);
                UpdateApplyButton();
            }

            private void OnHudThemeChanged(OptionButton.ItemSelectedEventArgs args)
            {
                _hudThemeOption.SelectId(args.Id);
                UpdateApplyButton();
            }

            private void OnApplyButtonPressed(BaseButton.ButtonEventArgs args)
            {
                _cfg.SetCVar(CVars.DisplayVSync, VSyncCheckBox.Pressed);
                SetConfigLightingQuality(LightingPresetOption.SelectedId);
                if (_hudThemeOption.SelectedId != _cfg.GetCVar(CCVars.HudTheme)) // Don't unnecessarily redraw the HUD
                {
                    _cfg.SetCVar(CCVars.HudTheme, _hudThemeOption.SelectedId);
                }

                _cfg.SetCVar(CVars.DisplayWindowMode,
                    (int) (FullscreenCheckBox.Pressed ? WindowMode.Fullscreen : WindowMode.Windowed));
                _cfg.SetCVar(CVars.DisplayUIScale, UIScaleOptions[_uiScaleOption.SelectedId]);
                _cfg.SetCVar(CCVars.ViewportStretch, _viewportStretchCheckBox.Pressed);
                _cfg.SetCVar(CCVars.ViewportFixedScaleFactor, (int) _viewportScaleSlider.Value);
                _cfg.SetCVar(CCVars.ViewportScaleRender, !_viewportLowResCheckBox.Pressed);
                _cfg.SaveToFile();
                UpdateApplyButton();
            }

            private void OnCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
            {
                UpdateApplyButton();
            }

            private void OnLightingQualityChanged(OptionButton.ItemSelectedEventArgs args)
            {
                LightingPresetOption.SelectId(args.Id);
                UpdateApplyButton();
            }

            private void UpdateApplyButton()
            {
                var isVSyncSame = VSyncCheckBox.Pressed == _cfg.GetCVar(CVars.DisplayVSync);
                var isFullscreenSame = FullscreenCheckBox.Pressed == ConfigIsFullscreen;
                var isLightingQualitySame = LightingPresetOption.SelectedId == GetConfigLightingQuality();
                var isHudThemeSame = _hudThemeOption.SelectedId == _cfg.GetCVar(CCVars.HudTheme);
                var isUIScaleSame = MathHelper.CloseTo(UIScaleOptions[_uiScaleOption.SelectedId], ConfigUIScale);
                var isVPStretchSame = _viewportStretchCheckBox.Pressed == _cfg.GetCVar(CCVars.ViewportStretch);
                var isVPScaleSame = (int) _viewportScaleSlider.Value == _cfg.GetCVar(CCVars.ViewportFixedScaleFactor);
                var isVPResSame = _viewportLowResCheckBox.Pressed == !_cfg.GetCVar(CCVars.ViewportScaleRender);

                ApplyButton.Disabled = isVSyncSame &&
                                       isFullscreenSame &&
                                       isLightingQualitySame &&
                                       isUIScaleSame &&
                                       isVPStretchSame &&
                                       isVPScaleSame &&
                                       isVPResSame &&
                                       isHudThemeSame;
            }

            private bool ConfigIsFullscreen =>
                _cfg.GetCVar(CVars.DisplayWindowMode) == (int) WindowMode.Fullscreen;

            private float ConfigUIScale => _cfg.GetCVar(CVars.DisplayUIScale);

            private int GetConfigLightingQuality()
            {
                var val = _cfg.GetCVar(CVars.DisplayLightMapDivider);
                var soft = _cfg.GetCVar(CVars.DisplaySoftShadows);
                if (val >= 8)
                {
                    return 0;
                }
                else if ((val >= 2) && !soft)
                {
                    return 1;
                }
                else if (val >= 2)
                {
                    return 2;
                }
                else
                {
                    return 3;
                }
            }

            private void SetConfigLightingQuality(int value)
            {
                switch (value)
                {
                    case 0:
                        _cfg.SetCVar(CVars.DisplayLightMapDivider, 8);
                        _cfg.SetCVar(CVars.DisplaySoftShadows, false);
                        break;
                    case 1:
                        _cfg.SetCVar(CVars.DisplayLightMapDivider, 2);
                        _cfg.SetCVar(CVars.DisplaySoftShadows, false);
                        break;
                    case 2:
                        _cfg.SetCVar(CVars.DisplayLightMapDivider, 2);
                        _cfg.SetCVar(CVars.DisplaySoftShadows, true);
                        break;
                    case 3:
                        _cfg.SetCVar(CVars.DisplayLightMapDivider, 1);
                        _cfg.SetCVar(CVars.DisplaySoftShadows, true);
                        break;
                }
            }

            private static int GetConfigUIScalePreset(float value)
            {
                for (var i = 0; i < UIScaleOptions.Length; i++)
                {
                    if (MathHelper.CloseTo(UIScaleOptions[i], value))
                    {
                        return i;
                    }
                }

                return 0;
            }

            private void UpdateViewportScale()
            {
                _viewportScaleBox.Visible = !_viewportStretchCheckBox.Pressed;
                _viewportScaleText.Text = Loc.GetString("ui-options-vp-scale", ("scale", _viewportScaleSlider.Value));
            }
        }
    }
}
