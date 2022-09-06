using System.Linq;
using Content.Client.CrewManifest;
using Content.Client.Eui;
using Content.Client.GameTicking.Managers;
using Content.Client.HUD.UI;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.CCVar;
using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.LateJoin
{
    public sealed class LateJoinGui : DefaultWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        public event Action<(EntityUid, string)> SelectedId;

        private readonly Dictionary<EntityUid, Dictionary<string, JobButton>> _jobButtons = new();
        private readonly Dictionary<EntityUid, Dictionary<string, BoxContainer>> _jobCategories = new();
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
                _consoleHost.ExecuteCommand($"joingame {CommandParsing.Escape(jobId)} {station}");
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
            var tracker = IoCManager.Resolve<PlayTimeTrackingManager>();

            if (!gameTicker.DisallowedLateJoin && gameTicker.StationNames.Count == 0)
                Logger.Warning("No stations exist, nothing to display in late-join GUI");

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

                if (_configManager.GetCVar<bool>(CCVars.CrewManifestWithoutEntity))
                {
                    var crewManifestButton = new Button()
                    {
                        Text = Loc.GetString("crew-manifest-button-label")
                    };
                    crewManifestButton.OnPressed += args =>
                    {
                        EntitySystem.Get<CrewManifestSystem>().RequestCrewManifest(id);
                    };

                    _base.AddChild(crewManifestButton);
                }

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

                foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
                {
                    var departmentName = Loc.GetString($"department-{department.ID}");
                    _jobCategories[id] = new Dictionary<string, BoxContainer>();
                    _jobButtons[id] = new Dictionary<string, JobButton>();
                    var stationAvailable = gameTicker.JobsAvailable[id];

                    var category = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Name = department.ID,
                        ToolTip = Loc.GetString("late-join-gui-jobs-amount-in-department-tooltip",
                            ("departmentName", departmentName))
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
                                Text = Loc.GetString("late-join-gui-department-jobs-label", ("departmentName", departmentName))
                            }
                        }
                    });

                    _jobCategories[id][department.ID] = category;
                    jobList.AddChild(category);
                    var jobsAvailable = new List<JobPrototype>();

                    foreach (var jobId in department.Roles)
                    {
                        if (!stationAvailable.ContainsKey(jobId)) continue;
                        jobsAvailable.Add(_prototypeManager.Index<JobPrototype>(jobId));
                    }

                    jobsAvailable.Sort((x, y) => -string.Compare(x.LocalizedName, y.LocalizedName, StringComparison.CurrentCultureIgnoreCase));

                    foreach (var prototype in jobsAvailable)
                    {
                        var value = stationAvailable[prototype.ID];
                        var jobButton = new JobButton(prototype.ID, value);

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

                        var specifier = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/Interface/Misc/job_icons.rsi"), prototype.Icon);
                        icon.Texture = specifier.Frame0();
                        jobSelector.AddChild(icon);

                        var jobLabel = new Label
                        {
                            Text = value != null ?
                                Loc.GetString("late-join-gui-job-slot-capped", ("jobName", prototype.LocalizedName), ("amount", value)) :
                                Loc.GetString("late-join-gui-job-slot-uncapped", ("jobName", prototype.LocalizedName))
                        };

                        jobSelector.AddChild(jobLabel);
                        jobButton.AddChild(jobSelector);
                        category.AddChild(jobButton);

                        jobButton.OnPressed += _ =>
                        {
                            SelectedId?.Invoke((id, jobButton.JobId));
                        };

                        string? reason = null;

                        if (!tracker.IsAllowed(prototype, out reason))
                        {
                            jobButton.Disabled = true;

                            if (!string.IsNullOrEmpty(reason))
                            {
                                jobButton.ToolTip = reason;
                            }
                        }
                        else if (value == 0)
                        {
                            jobButton.Disabled = true;
                        }

                        _jobButtons[id][prototype.ID] = jobButton;
                    }
                }
            }
        }

        private void JobsAvailableUpdated(IReadOnlyDictionary<EntityUid, Dictionary<string, uint?>> _)
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

    sealed class JobButton : ContainerButton
    {
        public string JobId { get; }
        public uint? Amount { get; }

        public JobButton(string jobId, uint? amount)
        {
            JobId = jobId;
            Amount = amount;
            AddStyleClass(StyleClassButton);
        }
    }
}
