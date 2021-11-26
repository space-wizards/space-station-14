using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameTicking.Managers;
using Content.Client.HUD.UI;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Client.Console;
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
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.LateJoin
{
    public sealed class LateJoinGui : SS14Window
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;

        public event Action<(StationId, string)> SelectedId;

        private readonly Dictionary<StationId, Dictionary<string, JobButton>> _jobButtons = new();
        private readonly Dictionary<StationId, Dictionary<string, BoxContainer>> _jobCategories = new();
        private readonly List<ScrollContainer> _jobLists = new();

        private readonly Control _base;

        public LateJoinGui()
        {
            MinSize = SetSize = (360, 560);
            IoCManager.InjectDependencies(this);

            var gameTicker = EntitySystem.Get<ClientGameTicker>();
            Title = Loc.GetString("late-join-gui-title");

            _base = new BoxContainer()
            {
                Orientation = LayoutOrientation.Vertical,
                VerticalExpand = true,
                Margin = new Thickness(0),
            };

            Contents.AddChild(_base);

            RebuildUI();

            SelectedId += x =>
            {
                var (station, jobId) = x;
                Logger.InfoS("latejoin", $"Late joining as ID: {jobId}");
                _consoleHost.ExecuteCommand($"joingame {CommandParsing.Escape(jobId)} {station.Id}");
                Close();
            };

            gameTicker.LobbyJobsAvailableUpdated += JobsAvailableUpdated;
        }

        private void RebuildUI()
        {
            _base.RemoveAllChildren();
            _jobLists.Clear();
            _jobButtons.Clear();
            _jobCategories.Clear();

            var gameTicker = EntitySystem.Get<ClientGameTicker>();
            foreach (var (id, name) in gameTicker.StationNames)
            {
                var jobList = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical
                };

                var collapseButton = new ContainerButton()
                {
                    HorizontalAlignment = HAlignment.Right,
                    ToggleMode = true,
                    Children =
                    {
                        new TextureRect
                        {
                            StyleClasses = { OptionButton.StyleClassOptionTriangle },
                            Margin = new Thickness(8, 0),
                            HorizontalAlignment = HAlignment.Center,
                            VerticalAlignment = VAlignment.Center,
                        }
                    }
                };

                _base.AddChild(new StripeBack()
                {
                    Children = {
                        new PanelContainer()
                        {
                            Children =
                            {
                                new Label()
                                {
                                    StyleClasses = { "LabelBig" },
                                    Text = name,
                                    Align = Label.AlignMode.Center,
                                },
                                collapseButton
                            }
                        }
                    }
                });
                var jobListScroll = new ScrollContainer()
                {
                    VerticalExpand = true,
                    Children = {jobList},
                    Visible = false,
                };

                if (_jobLists.Count == 0)
                    jobListScroll.Visible = true;

                _jobLists.Add(jobListScroll);

                _base.AddChild(jobListScroll);

                collapseButton.OnToggled += _ =>
                {
                    foreach (var section in _jobLists)
                    {
                        section.Visible = false;
                    }
                    jobListScroll.Visible = true;
                };

                var firstCategory = true;

                foreach (var job in gameTicker.JobsAvailable[id].OrderBy(x => x.Key))
                {
                    var prototype = _prototypeManager.Index<JobPrototype>(job.Key);
                    foreach (var department in prototype.Departments)
                    {
                        if (!_jobCategories.TryGetValue(id, out var _))
                            _jobCategories[id] = new Dictionary<string, BoxContainer>();
                        if (!_jobButtons.TryGetValue(id, out var _))
                            _jobButtons[id] = new Dictionary<string, JobButton>();
                        if (!_jobCategories[id].TryGetValue(department, out var category))
                        {
                            category = new BoxContainer
                            {
                                Orientation = LayoutOrientation.Vertical,
                                Name = department,
                                ToolTip = Loc.GetString("late-join-gui-jobs-amount-in-department-tooltip",
                                                        ("departmentName", department))
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
                                Children =
                                {
                                    new Label
                                    {
                                        StyleClasses = { "LabelBig" },
                                        Text = Loc.GetString("late-join-gui-department-jobs-label", ("departmentName", department))
                                    }
                                }
                            });

                            _jobCategories[id][department] = category;
                            jobList.AddChild(category);
                        }

                        var jobButton = new JobButton(prototype.ID, job.Value);

                        var jobSelector = new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            HorizontalExpand = true
                        };

                        var icon = new TextureRect
                        {
                            TextureScale = (2, 2),
                            Stretch = TextureRect.StretchMode.KeepCentered
                        };

                        if (prototype.Icon != null)
                        {
                            var specifier = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/Interface/Misc/job_icons.rsi"), prototype.Icon);
                            icon.Texture = specifier.Frame0();
                        }

                        jobSelector.AddChild(icon);

                        var jobLabel = new Label
                        {
                            Text = job.Value >= 0 ?
                                Loc.GetString("late-join-gui-job-slot-capped", ("jobName", prototype.Name), ("amount", job.Value)) :
                                Loc.GetString("late-join-gui-job-slot-uncapped", ("jobName", prototype.Name))
                        };

                        jobSelector.AddChild(jobLabel);
                        jobButton.AddChild(jobSelector);
                        category.AddChild(jobButton);

                        jobButton.OnPressed += _ =>
                        {
                            SelectedId?.Invoke((id, jobButton.JobId));
                        };

                        if (job.Value == 0)
                        {
                            jobButton.Disabled = true;
                        }

                        _jobButtons[id][prototype.ID] = jobButton;
                    }
                }
            }
        }

        private void JobsAvailableUpdated(IReadOnlyDictionary<StationId, Dictionary<string, int>> _)
        {
            RebuildUI();
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
        public int Amount { get; }

        public JobButton(string jobId, int amount)
        {
            JobId = jobId;
            Amount = amount;
            AddStyleClass(StyleClassButton);
        }
    }
}
