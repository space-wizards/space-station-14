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

        private readonly Dictionary<NetEntity, Dictionary<string, JobButton>> _jobButtons = new();
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
                    var stationAvailable = _gameTicker.JobsAvailable[id];
                    var jobsAvailable = new List<JobPrototype>();

                    foreach (var jobId in department.Roles)
                    {
                        if (!stationAvailable.ContainsKey(jobId))
                            continue;

                        jobsAvailable.Add(_prototypeManager.Index<JobPrototype>(jobId));
                    }

                    jobsAvailable.Sort((x, y) => -string.Compare(x.LocalizedName, y.LocalizedName, StringComparison.CurrentCultureIgnoreCase));

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
                        var jobButton = new JobButton(prototype.ID, value);

                        var jobSelector = new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            HorizontalExpand = true
                        };

                        var icon = new TextureRect
                        {
                            TextureScale = new Vector2(2, 2),
                            Stretch = TextureRect.StretchMode.KeepCentered
                        };

                        var jobIcon = _prototypeManager.Index<StatusIconPrototype>(prototype.Icon);
                        icon.Texture = _sprites.Frame0(jobIcon.Icon);
                        jobSelector.AddChild(icon);

                        var jobLabel = new Label
                        {
                            Margin = new Thickness(5f, 0, 0, 0),
                            Text = value != null ?
                                Loc.GetString("late-join-gui-job-slot-capped", ("jobName", prototype.LocalizedName), ("amount", value)) :
                                Loc.GetString("late-join-gui-job-slot-uncapped", ("jobName", prototype.LocalizedName)),
                        };

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

                        _jobButtons[id][prototype.ID] = jobButton;
                    }
                }
            }
        }

        private void JobsAvailableUpdated(IReadOnlyDictionary<NetEntity, Dictionary<string, uint?>> _)
        {
            RebuildUI();
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
