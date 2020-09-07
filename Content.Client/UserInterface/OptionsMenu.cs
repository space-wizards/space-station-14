using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    public sealed class OptionsMenu : SS14Window
    {
        private readonly Button ApplyButton;
        private readonly CheckBox VSyncCheckBox;
        private readonly CheckBox FullscreenCheckBox;
        private readonly OptionButton LightingPresetOption;
        private readonly IConfigurationManager configManager;

        protected override Vector2? CustomSize => (180, 160);

        public OptionsMenu(IConfigurationManager configMan)
        {
            configManager = configMan;

            Title = "Options";

            var vBox = new VBoxContainer();
            Contents.AddChild(vBox);
            //vBox.SetAnchorAndMarginPreset(LayoutPreset.Wide);

            VSyncCheckBox = new CheckBox {Text = "VSync"};
            vBox.AddChild(VSyncCheckBox);
            VSyncCheckBox.OnToggled += OnCheckBoxToggled;

            FullscreenCheckBox = new CheckBox {Text = "Fullscreen"};
            vBox.AddChild(FullscreenCheckBox);
            FullscreenCheckBox.OnToggled += OnCheckBoxToggled;

            vBox.AddChild(new Label {Text = "Lighting Quality"});

            LightingPresetOption = new OptionButton();
            LightingPresetOption.AddItem("Very Low");
            LightingPresetOption.AddItem("Low");
            LightingPresetOption.AddItem("Medium");
            LightingPresetOption.AddItem("High");
            vBox.AddChild(LightingPresetOption);
            LightingPresetOption.OnItemSelected += OnLightingQualityChanged;

            ApplyButton = new Button
            {
                Text = "Apply", TextAlign = Label.AlignMode.Center,
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };
            vBox.AddChild(ApplyButton);
            ApplyButton.OnPressed += OnApplyButtonPressed;

            VSyncCheckBox.Pressed = configManager.GetCVar<bool>("display.vsync");
            FullscreenCheckBox.Pressed = ConfigIsFullscreen;
            LightingPresetOption.SelectId(GetConfigLightingQuality());
        }

        private void OnApplyButtonPressed(BaseButton.ButtonEventArgs args)
        {
            configManager.SetCVar("display.vsync", VSyncCheckBox.Pressed);
            SetConfigLightingQuality(LightingPresetOption.SelectedId);
            configManager.SetCVar("display.windowmode",
                (int) (FullscreenCheckBox.Pressed ? WindowMode.Fullscreen : WindowMode.Windowed));
            configManager.SaveToFile();
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
            var isVSyncSame = VSyncCheckBox.Pressed == configManager.GetCVar<bool>("display.vsync");
            var isFullscreenSame = FullscreenCheckBox.Pressed == ConfigIsFullscreen;
            var isLightingQualitySame = LightingPresetOption.SelectedId == GetConfigLightingQuality();
            ApplyButton.Disabled = isVSyncSame && isFullscreenSame && isLightingQualitySame;
        }

        private bool ConfigIsFullscreen =>
            configManager.GetCVar<int>("display.windowmode") == (int) WindowMode.Fullscreen;

        private int GetConfigLightingQuality()
        {
            var val = configManager.GetCVar<int>("display.lightmapdivider");
            var soft = configManager.GetCVar<bool>("display.softshadows");
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
                    configManager.SetCVar("display.lightmapdivider", 8);
                    configManager.SetCVar("display.softshadows", false);
                    break;
                case 1:
                    configManager.SetCVar("display.lightmapdivider", 2);
                    configManager.SetCVar("display.softshadows", false);
                    break;
                case 2:
                    configManager.SetCVar("display.lightmapdivider", 2);
                    configManager.SetCVar("display.softshadows", true);
                    break;
                case 3:
                    configManager.SetCVar("display.lightmapdivider", 1);
                    configManager.SetCVar("display.softshadows", true);
                    break;
            }
        }
    }
}
