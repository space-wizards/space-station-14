using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.UserInterface
{
    public sealed partial class OptionsMenu
    {
        private sealed class GraphicsControl : Control
        {
            private readonly IConfigurationManager _cfg;

            private readonly Button ApplyButton;
            private readonly CheckBox VSyncCheckBox;
            private readonly CheckBox FullscreenCheckBox;
            private readonly OptionButton LightingPresetOption;


            public GraphicsControl(IConfigurationManager cfg)
            {
                _cfg = cfg;
                var vBox = new VBoxContainer();

                var contents = new VBoxContainer();

                VSyncCheckBox = new CheckBox {Text = Loc.GetString("VSync")};
                contents.AddChild(VSyncCheckBox);
                VSyncCheckBox.OnToggled += OnCheckBoxToggled;

                FullscreenCheckBox = new CheckBox {Text = Loc.GetString("Fullscreen")};
                contents.AddChild(FullscreenCheckBox);
                FullscreenCheckBox.OnToggled += OnCheckBoxToggled;

                LightingPresetOption = new OptionButton {CustomMinimumSize = (100, 0)};
                LightingPresetOption.AddItem(Loc.GetString("Very Low"));
                LightingPresetOption.AddItem(Loc.GetString("Low"));
                LightingPresetOption.AddItem(Loc.GetString("Medium"));
                LightingPresetOption.AddItem(Loc.GetString("High"));
                LightingPresetOption.OnItemSelected += OnLightingQualityChanged;

                var lightingHBox = new HBoxContainer
                {
                    Children =
                    {
                        new Label {Text = Loc.GetString("Lighting Quality:")},
                        new Control {CustomMinimumSize = (4, 0)},
                        LightingPresetOption
                    }
                };
                contents.AddChild(lightingHBox);

                ApplyButton = new Button
                {
                    Text = Loc.GetString("Apply"), TextAlign = Label.AlignMode.Center,
                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd
                };

                var resourceCache = IoCManager.Resolve<IResourceCache>();

                contents.AddChild(new Placeholder(resourceCache)
                {
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    PlaceholderText = "UI Scaling"
                });

                contents.AddChild(new Placeholder(resourceCache)
                {
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    PlaceholderText = "Viewport settings"
                });

                vBox.AddChild(new MarginContainer
                {
                    MarginLeftOverride = 2,
                    MarginTopOverride = 2,
                    MarginRightOverride = 2,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    Children =
                    {
                        contents
                    }
                });

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

                VSyncCheckBox.Pressed = _cfg.GetCVar<bool>("display.vsync");
                FullscreenCheckBox.Pressed = ConfigIsFullscreen;
                LightingPresetOption.SelectId(GetConfigLightingQuality());


                AddChild(vBox);
            }

            private void OnApplyButtonPressed(BaseButton.ButtonEventArgs args)
            {
                _cfg.SetCVar("display.vsync", VSyncCheckBox.Pressed);
                SetConfigLightingQuality(LightingPresetOption.SelectedId);
                _cfg.SetCVar("display.windowmode",
                    (int) (FullscreenCheckBox.Pressed ? WindowMode.Fullscreen : WindowMode.Windowed));
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
                var isVSyncSame = VSyncCheckBox.Pressed == _cfg.GetCVar<bool>("display.vsync");
                var isFullscreenSame = FullscreenCheckBox.Pressed == ConfigIsFullscreen;
                var isLightingQualitySame = LightingPresetOption.SelectedId == GetConfigLightingQuality();
                ApplyButton.Disabled = isVSyncSame && isFullscreenSame && isLightingQualitySame;
            }

            private bool ConfigIsFullscreen =>
                _cfg.GetCVar<int>("display.windowmode") == (int) WindowMode.Fullscreen;

            private int GetConfigLightingQuality()
            {
                var val = _cfg.GetCVar<int>("display.lightmapdivider");
                var soft = _cfg.GetCVar<bool>("display.softshadows");
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
                        _cfg.SetCVar("display.lightmapdivider", 8);
                        _cfg.SetCVar("display.softshadows", false);
                        break;
                    case 1:
                        _cfg.SetCVar("display.lightmapdivider", 2);
                        _cfg.SetCVar("display.softshadows", false);
                        break;
                    case 2:
                        _cfg.SetCVar("display.lightmapdivider", 2);
                        _cfg.SetCVar("display.softshadows", true);
                        break;
                    case 3:
                        _cfg.SetCVar("display.lightmapdivider", 1);
                        _cfg.SetCVar("display.softshadows", true);
                        break;
                }
            }
        }
    }
}
