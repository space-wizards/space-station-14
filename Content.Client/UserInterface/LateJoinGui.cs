using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Content.Shared.Jobs;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Robust.Client.UserInterface.CustomControls
{
    public sealed class LateJoinGui : SS14Window
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IClientConsole _console;
#pragma warning restore 649
        /*private readonly Button ApplyButton;
        private readonly CheckBox VSyncCheckBox;
        private readonly CheckBox FullscreenCheckBox;
        private readonly CheckBox HighResLightsCheckBox;*/
        //private readonly IConfigurationManager configManager;

        protected override Vector2? CustomSize => (360, 560);

        public event Action<string> SelectedId;

        private Dictionary<string, int> AvailablePositions;

        public LateJoinGui(/*IConfigurationManager configMan*/)
        {
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("Late Join");

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

            foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>().OrderBy(j => j.Name))
            {
                var jobButton = new JobButton
                {
                    JobId = job.ID
                };
                var jobSelector = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand
                };

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
                    Text = job.Name
                };
                jobSelector.AddChild(jobLabel);

                Logger.InfoS("latejoin", $"Job: {job.ID}, Positions: {job.TotalPositions}");
                var jobPrototype = _prototypeManager.Index<JobPrototype>(job.ID);
                Logger.InfoS("latejoin", $"Weird Job: {jobPrototype.ID}, Positions: {job.TotalPositions}");
                if(jobPrototype.TotalPositions == 0) //TODO: Make this work. GetAvailablePositions() is serverside
                {
                    Logger.InfoS("latejoin", $"Disabling {job.ID}");
                    jobButton.Disabled = true;
                }
                jobButton.AddChild(jobSelector);
                jobList.AddChild(jobButton);
                //jobButton.OnPressed += OnJobButtonPressed;
                jobButton.OnPressed += args =>
                {
                    SelectedId?.Invoke(jobButton.JobId);
                };
                /*jobButton.OnItemSelected += args =>
                {
                    _optionButton.SelectId(args.Id);
                    PriorityChanged?.Invoke(Priority);
                };*/
            }

            SelectedId += jobId =>
            {
                Logger.InfoS("latejoin", $"Late joining as ID: {jobId}");
                _console.ProcessCommand($"joingame {CommandParsing.Escape(jobId)}");
                Close();
            };


        }

        public string ReturnId()
        {
            return SelectedId.ToString();
        }

    }
    class JobButton : ContainerButton
    {
        public string JobId { get; set; }
        public JobButton()
        {
            AddStyleClass(StyleClassButton);
        }
    }
}
