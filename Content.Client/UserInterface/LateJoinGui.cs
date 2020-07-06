using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Content.Shared.Jobs;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;


namespace Robust.Client.UserInterface.CustomControls
{
    public sealed class LateJoinGui : SS14Window
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649
        /*private readonly Button ApplyButton;
        private readonly CheckBox VSyncCheckBox;
        private readonly CheckBox FullscreenCheckBox;
        private readonly CheckBox HighResLightsCheckBox;*/
        //private readonly IConfigurationManager configManager;

        protected override Vector2? CustomSize => (360, 560);

        public LateJoinGui(/*IConfigurationManager configMan*/)
        {
            IoCManager.InjectDependencies(this);
            //configManager = configMan;

            Title = "Late Join";
            var jobList = new VBoxContainer();
            var vBox = new VBoxContainer
            {
                    Children =
                    {
                        new ScrollContainer
                        {
                            SizeFlagsVertical = SizeFlags.FillExpand,
                            Children =
                            {
                                jobList
                            }
                        }
                    }
                };
            Contents.AddChild(vBox);

            /*VSyncCheckBox = new CheckBox {Text = "VSync"};
            vBox.AddChild(VSyncCheckBox);
            VSyncCheckBox.OnToggled += OnCheckBoxToggled;

            FullscreenCheckBox = new CheckBox {Text = "Fullscreen"};
            vBox.AddChild(FullscreenCheckBox);
            FullscreenCheckBox.OnToggled += OnCheckBoxToggled;

            HighResLightsCheckBox = new CheckBox {Text = "High-Res Lights"};
            vBox.AddChild(HighResLightsCheckBox);
            HighResLightsCheckBox.OnToggled += OnCheckBoxToggled;

            ApplyButton = new Button
            {
                Text = "Apply", TextAlign = Label.AlignMode.Center,
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };
            vBox.AddChild(ApplyButton);
            ApplyButton.OnPressed += OnApplyButtonPressed;

            VSyncCheckBox.Pressed = configManager.GetCVar<bool>("display.vsync");
            HighResLightsCheckBox.Pressed = configManager.GetCVar<bool>("display.highreslights");
            FullscreenCheckBox.Pressed = ConfigIsFullscreen;*/

            foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>().OrderBy(j => j.Name))
            {
                var jobSelector = new HBoxContainer();

                var icon = new TextureRect
                {
                    TextureScale = (2, 2),
                    Stretch = TextureRect.StretchMode.KeepCentered
                };

                if (job.Icon != null)
                {
                    var specifier = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/job_icons.rsi"), job.Icon);
                    icon.Texture = specifier.Frame0();
                }
                jobSelector.AddChild(icon);

                var jobLabel = new Label
                {
                    //SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                    //SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    Text = job.Name
                };
                jobSelector.AddChild(jobLabel);

                var button = new JobButton
                {
                    Text = Loc.GetString("Select"),
                    ToolTip = Loc.GetString($"Join as {job.Name}."),
                    TextAlign = Label.AlignMode.Center,
                    JobId = job.ID
                };
                /*if(job.TotalPositions == 0) //TODO: Make this work. GetAvailablePositions() is serverside
                {
                    button.Disabled = true;
                }*/
                jobSelector.AddChild(button);
                jobList.AddChild(jobSelector);
            }
        }

        private void OnApplyButtonPressed(BaseButton.ButtonEventArgs args)
        {
            /*configManager.SetCVar("display.vsync", VSyncCheckBox.Pressed);
            configManager.SetCVar("display.highreslights", HighResLightsCheckBox.Pressed);
            configManager.SetCVar("display.windowmode",
                (int) (FullscreenCheckBox.Pressed ? WindowMode.Fullscreen : WindowMode.Windowed));
            configManager.SaveToFile();
            UpdateApplyButton();*/
        }

        private void OnCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
        {
            UpdateApplyButton();
        }

        private void UpdateApplyButton()
        {
            //var isVSyncSame = VSyncCheckBox.Pressed == configManager.GetCVar<bool>("display.vsync");
            //var isHighResLightsSame = HighResLightsCheckBox.Pressed == configManager.GetCVar<bool>("display.highreslights");
            //var isFullscreenSame = FullscreenCheckBox.Pressed == ConfigIsFullscreen;
            //ApplyButton.Disabled = isVSyncSame && isHighResLightsSame && isFullscreenSame;
        }
        private class JobButton : Button
        {
            public string JobId { get; set; }
        }
    }
}
