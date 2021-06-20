using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameTicking.Managers;
using Content.Shared.Roles;
using Robust.Client.Console;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.LateJoin
{
    public sealed class LateJoinGui : SS14Window
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;

        public event Action<string>? SelectedId;

        private readonly Dictionary<string, JobButton> _jobButtons = new();
        private readonly Dictionary<string, VBoxContainer> _jobCategories = new();

        public LateJoinGui()
        {
            MinSize = SetSize = (360, 560);
            IoCManager.InjectDependencies(this);

            var gameTicker = EntitySystem.Get<ClientGameTicker>();

            Title = Loc.GetString("Late Join");

            var jobList = new VBoxContainer();
            var vBox = new VBoxContainer
            {
                Children =
                {
                    new ScrollContainer
                    {
                        VerticalExpand = true,
                        Children =
                        {
                            jobList
                        }
                    }
                }
            };

            Contents.AddChild(vBox);

            var firstCategory = true;

            foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>().OrderBy(j => j.Name))
            {
                foreach (var department in job.Departments)
                {
                    if (!_jobCategories.TryGetValue(department, out var category))
                    {
                        category = new VBoxContainer
                        {
                            Name = department,
                            ToolTip = Loc.GetString("Jobs in the {0} department", department)
                        };

                        if (firstCategory)
                        {
                            firstCategory = false;
                        }
                        else
                        {
                            category.AddChild(new Control
                            {
                                MinSize = new Vector2(0, 23),
                            });
                        }

                        category.AddChild(new PanelContainer
                        {
                            PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#464966")},
                            Children =
                            {
                                new Label
                                {
                                    Text = Loc.GetString("{0} jobs", department)
                                }
                            }
                        });

                        _jobCategories[department] = category;
                        jobList.AddChild(category);
                    }

                    var jobButton = new JobButton(job.ID);

                    var jobSelector = new HBoxContainer
                    {
                        HorizontalExpand = true
                    };

                    var icon = new TextureRect
                    {
                        TextureScale = (2, 2),
                        Stretch = TextureRect.StretchMode.KeepCentered
                    };

                    if (job.Icon != null)
                    {
                        var specifier = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/Interface/Misc/job_icons.rsi"), job.Icon);
                        icon.Texture = specifier.Frame0();
                    }

                    jobSelector.AddChild(icon);

                    var jobLabel = new Label
                    {
                        Text = job.Name
                    };

                    jobSelector.AddChild(jobLabel);
                    jobButton.AddChild(jobSelector);
                    category.AddChild(jobButton);

                    jobButton.OnPressed += _ =>
                    {
                        SelectedId?.Invoke(jobButton.JobId);
                    };

                    if (!gameTicker.JobsAvailable.Contains(job.ID))
                    {
                        jobButton.Disabled = true;
                    }

                    _jobButtons[job.ID] = jobButton;
                }
            }

            SelectedId += jobId =>
            {
                Logger.InfoS("latejoin", $"Late joining as ID: {jobId}");
                _consoleHost.ExecuteCommand($"joingame {CommandParsing.Escape(jobId)}");
                Close();
            };

            gameTicker.LobbyJobsAvailableUpdated += JobsAvailableUpdated;
        }

        private void JobsAvailableUpdated(IReadOnlyList<string> jobs)
        {
            foreach (var (id, button) in _jobButtons)
            {
                button.Disabled = !jobs.Contains(id);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                EntitySystem.Get<ClientGameTicker>().LobbyJobsAvailableUpdated -= JobsAvailableUpdated;
                _jobButtons.Clear();
                _jobCategories.Clear();
            }
        }
    }

    class JobButton : ContainerButton
    {
        public string JobId { get; }

        public JobButton(string jobId)
        {
            JobId = jobId;
            AddStyleClass(StyleClassButton);
        }
    }
}
