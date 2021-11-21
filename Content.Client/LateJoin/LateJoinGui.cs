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
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.LateJoin
{
    public sealed class LateJoinGui : SS14Window
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;

        public event Action<(uint, string)>? SelectedId;

        private readonly Dictionary<uint, Dictionary<string, JobButton>> _jobButtons = new();
        private readonly Dictionary<uint, Dictionary<string, BoxContainer>> _jobCategories = new();
        private readonly Dictionary<uint, BoxContainer> _stationCategories = new();

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
            };

            Contents.AddChild(_base);

            RebuildUI();

            SelectedId += x =>
            {
                var (station, jobId) = x;
                Logger.InfoS("latejoin", $"Late joining as ID {jobId} in station {station}");
                _consoleHost.ExecuteCommand($"joingame {CommandParsing.Escape(jobId)} {station}");
                Close();
            };

            gameTicker.LobbyJobsAvailableUpdated += JobsAvailableUpdated;
        }

        private void RebuildUI()
        {
            _base.RemoveAllChildren();
            var gameTicker = EntitySystem.Get<ClientGameTicker>();
            Logger.Debug("uh");
            foreach (var (id, name) in gameTicker.StationNames)
            {
                Logger.Debug($"Starting on station {name}");
                var jobList = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical
                };

                _base.AddChild(new Label()
                {
                    Text = $"NTSS {name}"
                });
                _base.AddChild(new ScrollContainer()
                {
                    VerticalExpand = true,
                    Children = { jobList }
                });

                var firstCategory = true;

                foreach (var job in gameTicker.JobsAvailable[id].OrderBy(x => x.Key))
                {
                    Logger.Debug($"Adding job {job.Key}");
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
                                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#464966")},
                                Children =
                                {
                                    new Label
                                    {
                                        Text = Loc.GetString("late-join-gui-department-jobs-label", ("departmentName", department))
                                    }
                                }
                            });

                            _jobCategories[id][department] = category;
                            jobList.AddChild(category);
                        }

                        var jobButton = new JobButton(prototype.ID);

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
                            Text = prototype.Name
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

        private void JobsAvailableUpdated(IReadOnlyDictionary<uint, Dictionary<string, int>> _)
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

        public JobButton(string jobId)
        {
            JobId = jobId;
            AddStyleClass(StyleClassButton);
        }
    }
}
