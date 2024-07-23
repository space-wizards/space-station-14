using System.Linq;
using System.Numerics;
using Content.Client.CrewManifest;
using Content.Client.GameTicking.Managers;
using Content.Client.UserInterface.Controls;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
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
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
        [Dependency] private readonly JobRequirementsManager _jobRequirements = default!;

        public event Action<(NetEntity, string)> SelectedId;

        private readonly ClientGameTicker _gameTicker;
        private readonly SpriteSystem _sprites;
        private readonly CrewManifestSystem _crewManifest;

        private readonly Dictionary<NetEntity, Dictionary<string, List<JobButton>>> _jobButtons = new();
        private readonly Dictionary<NetEntity, Dictionary<string, BoxContainer>> _jobCategories = new();
        private readonly List<ScrollContainer> _jobLists = new();

        private readonly Control _base;

        public LateJoinGui()
        {
            MinSize = SetSize = new Vector2(360, 560);
            IoCManager.InjectDependencies(this);
            _sprites = _entitySystem.GetEntitySystem<SpriteSystem>();
            _crewManifest = _entitySystem.GetEntitySystem<CrewManifestSystem>();
            _gameTicker = _entitySystem.GetEntitySystem<ClientGameTicker>();

            Title = Loc.GetString("late-join-gui-title");

            _base = new BoxContainer()
            {
                Orientation = LayoutOrientation.Vertical,
                VerticalExpand = true,
            };

            Contents.AddChild(_base);

            _jobRequirements.Updated += RebuildUI;
            RebuildUI();

            SelectedId += x =>
            {
                var (station, jobId) = x;
                Logger.InfoS("latejoin", $"Late joining as ID: {jobId}");
                _consoleHost.ExecuteCommand($"joingame {CommandParsing.Escape(jobId)} {station}");
                Close();
            };

            _gameTicker.LobbyJobsAvailableUpdated += JobsAvailableUpdated;
        }

        private void RebuildUI()
        {
            _base.RemoveAllChildren();
            _jobLists.Clear();
            _jobButtons.Clear();
            _jobCategories.Clear();

            if (!_gameTicker.DisallowedLateJoin && _gameTicker.StationNames.Count == 0)
                Logger.Warning("No stations exist, nothing to display in late-join GUI");

            foreach (var (id, name) in _gameTicker.StationNames)
            {
                var jobList = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Margin = new Thickness(0, 0, 5f, 0),
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
                    Children =
                    {
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

                if (_configManager.GetCVar(CCVars.CrewManifestWithoutEntity))
                {
                    var crewManifestButton = new Button()
                    {
                        Text = Loc.GetString("crew-manifest-button-label")
                    };
                    crewManifestButton.OnPressed += _ => _crewManifest.RequestCrewManifest(id);

                    _base.AddChild(crewManifestButton);
                }

                var jobListScroll = new ScrollContainer()
                {
                    VerticalExpand = true,
                    Children = { jobList },
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
                var departments = _prototypeManager.EnumeratePrototypes<DepartmentPrototype>().ToArray();
                Array.Sort(departments, DepartmentUIComparer.Instance);

                _jobButtons[id] = new Dictionary<string, List<JobButton>>();

                foreach (var department in departments)
                {
                    var departmentName = Loc.GetString($"department-{department.ID}");
                    _jobCategories[id] = new Dictionary<string, BoxContainer>();
                    var stationAvailable = _gameTicker.JobsAvailable[id];
                    var jobsAvailable = new List<JobPrototype>();

                    foreach (var jobId in department.Roles)
                    {
                        if (!stationAvailable.ContainsKey(jobId))
                            continue;

                        jobsAvailable.Add(_prototypeManager.Index<JobPrototype>(jobId));
                    }

                    jobsAvailable.Sort(JobUIComparer.Instance);

                    // Do not display departments with no jobs available.
                    if (jobsAvailable.Count == 0)
                        continue;

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

                    foreach (var prototype in jobsAvailable)
                    {
                        var value = stationAvailable[prototype.ID];

                        var jobLabel = new Label
                        {
                            Margin = new Thickness(5f, 0, 0, 0)
                        };

                        var jobButton = new JobButton(jobLabel, prototype.ID, prototype.LocalizedName, value);

                        var jobSelector = new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            HorizontalExpand = true
                        };

                        var icon = new TextureRect
                        {
                            TextureScale = new Vector2(2, 2),
                            VerticalAlignment = VAlignment.Center
                        };

                        var jobIcon = _prototypeManager.Index(prototype.Icon);
                        icon.Texture = _sprites.Frame0(jobIcon.Icon);
                        jobSelector.AddChild(icon);

                        jobSelector.AddChild(jobLabel);
                        jobButton.AddChild(jobSelector);
                        category.AddChild(jobButton);

                        jobButton.OnPressed += _ => SelectedId.Invoke((id, jobButton.JobId));

                        if (!_jobRequirements.IsAllowed(prototype, out var reason))
                        {
                            jobButton.Disabled = true;

                            if (!reason.IsEmpty)
                            {
                                var tooltip = new Tooltip();
                                tooltip.SetMessage(reason);
                                jobButton.TooltipSupplier = _ => tooltip;
                            }

                            jobSelector.AddChild(new TextureRect
                            {
                                TextureScale = new Vector2(0.4f, 0.4f),
                                Stretch = TextureRect.StretchMode.KeepCentered,
                                Texture = _sprites.Frame0(new SpriteSpecifier.Texture(new ("/Textures/Interface/Nano/lock.svg.192dpi.png"))),
                                HorizontalExpand = true,
                                HorizontalAlignment = HAlignment.Right,
                            });
                        }
                        else if (value == 0)
                        {
                            jobButton.Disabled = true;
                        }

                        if (!_jobButtons[id].ContainsKey(prototype.ID))
                        {
                            _jobButtons[id][prototype.ID] = new List<JobButton>();
                        }

                        _jobButtons[id][prototype.ID].Add(jobButton);
                    }
                }
            }
        }

        private void JobsAvailableUpdated(IReadOnlyDictionary<NetEntity, Dictionary<ProtoId<JobPrototype>, int?>> updatedJobs)
        {
            foreach (var stationEntries in updatedJobs)
            {
                if (_jobButtons.ContainsKey(stationEntries.Key))
                {
                    var jobsAvailable = stationEntries.Value;

                    var existingJobEntries = _jobButtons[stationEntries.Key];
                    foreach (var existingJobEntry in existingJobEntries)
                    {
                        if (jobsAvailable.ContainsKey(existingJobEntry.Key))
                        {
                            var updatedJobValue = jobsAvailable[existingJobEntry.Key];
                            foreach (var matchingJobButton in existingJobEntry.Value)
                            {
                                if (matchingJobButton.Amount != updatedJobValue)
                                {
                                    matchingJobButton.RefreshLabel(updatedJobValue);
                                    matchingJobButton.Disabled |= matchingJobButton.Amount == 0;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _jobRequirements.Updated -= RebuildUI;
                _gameTicker.LobbyJobsAvailableUpdated -= JobsAvailableUpdated;
                _jobButtons.Clear();
                _jobCategories.Clear();
            }
        }
    }

    sealed class JobButton : ContainerButton
    {
        public Label JobLabel { get; }
        public string JobId { get; }
        public string JobLocalisedName { get; }
        public int? Amount { get; private set; }
        private bool _initialised = false;

        public JobButton(Label jobLabel, ProtoId<JobPrototype> jobId, string jobLocalisedName, int? amount)
        {
            JobLabel = jobLabel;
            JobId = jobId;
            JobLocalisedName = jobLocalisedName;
            RefreshLabel(amount);
            AddStyleClass(StyleClassButton);
            _initialised = true;
        }

        public void RefreshLabel(int? amount)
        {
            if (Amount == amount && _initialised)
            {
                return;
            }
            Amount = amount;

            JobLabel.Text = Amount != null ?
                Loc.GetString("late-join-gui-job-slot-capped", ("jobName", JobLocalisedName), ("amount", Amount)) :
                Loc.GetString("late-join-gui-job-slot-uncapped", ("jobName", JobLocalisedName));
        }
    }
}
