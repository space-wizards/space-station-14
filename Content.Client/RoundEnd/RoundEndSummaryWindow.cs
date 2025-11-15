using System.Linq;
using System.Numerics;
using Content.Client.Message;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.RoundEnd
{
    /// <summary>
    /// Window displaying round end information including player manifest with job icons.
    /// </summary>
    public sealed class RoundEndSummaryWindow : DefaultWindow
    {
        private readonly IEntityManager _entityManager;
        private readonly IPrototypeManager _prototypeManager;
        private readonly SpriteSystem _spriteSystem;
        private readonly ISawmill _sawmill;
        public int RoundId;
        private readonly RoundEndMessageEvent.RoundEndPlayerInfo[] _playersInfo;
        private GridContainer _playerGrid = null!;
        private readonly List<SortButton> _sortButtons = [];
        private string _searchText = string.Empty;

        private static readonly ProtoId<JobIconPrototype> ObserverIcon = "JobIconUnknown";
        private static readonly ProtoId<JobIconPrototype> UnknownIcon = "JobIconUnknown";

        private enum SortField
        {
            ICName,
            JobIcon,
            Role,
            PlayerType,
            OOCName
        }

        private SortField _currentSortField = SortField.PlayerType;
        private bool _sortDescending = false;

        public RoundEndSummaryWindow(string gm, string roundEnd, TimeSpan roundTimeSpan, int roundId,
            RoundEndMessageEvent.RoundEndPlayerInfo[] info, IEntityManager entityManager)
        {
            _entityManager = entityManager;
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            _spriteSystem = entityManager.System<SpriteSystem>();
            _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("roundend");
            _playersInfo = info;

            MinSize = SetSize = new Vector2(720, 580);

            Title = Loc.GetString("round-end-summary-window-title");

            // The round end window is split into two tabs, one about the round stats
            // and the other is a list of RoundEndPlayerInfo for each player.
            // This tab would be a good place for things like: "x many people died.",
            // "clown slipped the crew x times.", "x shots were fired this round.", etc.
            // Also good for serious info.

            RoundId = roundId;
            var roundEndTabs = new TabContainer();
            roundEndTabs.AddChild(MakeRoundEndSummaryTab(gm, roundEnd, roundTimeSpan, roundId));
            roundEndTabs.AddChild(MakePlayerManifestTab());

            ContentsContainer.AddChild(roundEndTabs);

            OpenCenteredRight();
            MoveToFront();
        }

        private static BoxContainer MakeRoundEndSummaryTab(string gamemode, string roundEnd, TimeSpan roundDuration, int roundId)
        {
            var roundEndSummaryTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-round-end-summary-tab-title")
            };

            var roundEndSummaryContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10)
            };
            var roundEndSummaryContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            //Gamemode Name
            var gamemodeLabel = new RichTextLabel();
            var gamemodeMessage = new FormattedMessage();
            gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-round-id-label", ("roundId", roundId)));
            gamemodeMessage.AddText(" ");
            gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-gamemode-name-label", ("gamemode", gamemode)));
            gamemodeLabel.SetMessage(gamemodeMessage);
            roundEndSummaryContainer.AddChild(gamemodeLabel);

            //Duration
            var roundTimeLabel = new RichTextLabel();
            roundTimeLabel.SetMarkup(Loc.GetString("round-end-summary-window-duration-label",
                                                   ("hours", roundDuration.Hours),
                                                   ("minutes", roundDuration.Minutes),
                                                   ("seconds", roundDuration.Seconds)));
            roundEndSummaryContainer.AddChild(roundTimeLabel);

            //Round end text
            if (!string.IsNullOrEmpty(roundEnd))
            {
                var roundEndLabel = new RichTextLabel();
                roundEndLabel.SetMarkup(roundEnd);
                roundEndSummaryContainer.AddChild(roundEndLabel);
            }

            roundEndSummaryContainerScrollbox.AddChild(roundEndSummaryContainer);
            roundEndSummaryTab.AddChild(roundEndSummaryContainerScrollbox);

            return roundEndSummaryTab;
        }

        private BoxContainer MakePlayerManifestTab()
        {
            var playerManifestTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-player-manifest-tab-title")
            };

            // Search container
            var searchContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Margin = new Thickness(10, 10, 10, 5)
            };

            var searchLabel = new Label
            {
                Text = "Filter: ",
                VerticalAlignment = VAlignment.Center,
                MinSize = new Vector2(40, 1)
            };

            var searchBar = new LineEdit
            {
                PlaceHolder = Loc.GetString("round-end-summary-window-player-manifest-tab-search-placeholder"),
                HorizontalExpand = true,
                MinSize = new Vector2(200, 1)
            };

            searchBar.OnTextChanged += OnSearchTextChanged;

            searchContainer.AddChild(searchLabel);
            searchContainer.AddChild(searchBar);
            playerManifestTab.AddChild(searchContainer);

            // Header with sort buttons
            var headerContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Margin = new Thickness(10, 5, 10, 5)
            };

            var icNameButton = CreateSortButton("round-end-summary-window-player-manifest-tab-sort-character", SortField.ICName);
            var roleButton = CreateSortButton("round-end-summary-window-player-manifest-tab-sort-role", SortField.Role);
            var playerTypeButton = CreateSortButton("round-end-summary-window-player-manifest-tab-sort-player-type", SortField.PlayerType);
            var oocNameButton = CreateSortButton("round-end-summary-window-player-manifest-tab-sort-player", SortField.OOCName);

            playerTypeButton.SetSortIndicator(true);
            headerContainer.AddChild(icNameButton);

            // Add small spacer between buttons
            headerContainer.AddChild(new Control
            {
                MinSize = new Vector2(5, 1),
                HorizontalExpand = false
            });

            headerContainer.AddChild(roleButton);

            // Add small spacer between buttons
            headerContainer.AddChild(new Control
            {
                MinSize = new Vector2(5, 1),
                HorizontalExpand = false
            });

            headerContainer.AddChild(playerTypeButton);

            // Add small spacer between buttons
            headerContainer.AddChild(new Control
            {
                MinSize = new Vector2(5, 1),
                HorizontalExpand = false
            });

            headerContainer.AddChild(oocNameButton);

            playerManifestTab.AddChild(headerContainer);

            var scrollContainer = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10, 0, 10, 10),
            };

            _playerGrid = new GridContainer
            {
                Columns = 6, // Player Sprite,IC Name,Job Icon,Role,Player Type,OOC Name
                HorizontalExpand = true,
            };

            RefreshPlayerList();

            scrollContainer.AddChild(_playerGrid);
            playerManifestTab.AddChild(scrollContainer);

            return playerManifestTab;
        }

        private SortButton CreateSortButton(string text, SortField field)
        {
            var button = new SortButton(Loc.GetString(text), field);
            button.OnPressed += _ => SortBy(field);
            _sortButtons.Add(button);
            return button;
        }

        /// <summary>
        /// Handles sorting by the specified field, toggling direction if the same field is clicked
        /// </summary>
        private void SortBy(SortField field)
        {
            if (_currentSortField == field)
            {
                _sortDescending = !_sortDescending;
            }
            else
            {
                _currentSortField = field;
                _sortDescending = false;
            }

            foreach (var button in _sortButtons)
            {
                button.SetSortIndicator(button.Field == _currentSortField, _sortDescending);
            }

            RefreshPlayerList();
        }

        /// <summary>
        /// Refreshes the player list grid by clearing it and repopulating with sorted player data
        /// </summary>
        private void RefreshPlayerList()
        {
            _playerGrid.RemoveAllChildren();

            var sortedPlayers = GetSortedPlayers();
            foreach (var playerInfo in sortedPlayers)
            {
                AddPlayerRow(playerInfo);
            }
        }

        private IEnumerable<RoundEndMessageEvent.RoundEndPlayerInfo> GetSortedPlayers()
        {
            // First filter players based on search text
            var filteredPlayers = string.IsNullOrEmpty(_searchText)
                ? _playersInfo
                : _playersInfo.Where(PlayerMatchesSearch);

            var sortKeyExtractors = new Dictionary<SortField, Func<RoundEndMessageEvent.RoundEndPlayerInfo, object>>
            {
                [SortField.ICName] = p => (p.PlayerICName ?? p.PlayerOOCName ?? string.Empty).ToLowerInvariant(),
                [SortField.OOCName] = p => (p.PlayerOOCName ?? string.Empty).ToLowerInvariant(),
                [SortField.Role] = p => (p.Observer ? "zzz_observer" : p.Role ?? string.Empty).ToLowerInvariant(),
                [SortField.JobIcon] = p => (p.Observer ? "zzz_observer" : p.Role ?? string.Empty).ToLowerInvariant(),
                [SortField.PlayerType] = p => GetPlayerTypeSortKey(p)
            };

            var primaryKey = sortKeyExtractors[_currentSortField];

            static object SecondaryKey(RoundEndMessageEvent.RoundEndPlayerInfo p) => (p.PlayerICName ?? p.PlayerOOCName ?? string.Empty).ToLowerInvariant();

            return _sortDescending
                ? filteredPlayers.OrderByDescending(primaryKey).ThenByDescending(SecondaryKey)
                : filteredPlayers.OrderBy(primaryKey).ThenBy(SecondaryKey);
        }

        /// <summary>
        /// Adds a single player row to the grid with all columns (sprite, IC name, job icon, role, player type, OOC name)
        /// </summary>
        private void AddPlayerRow(RoundEndMessageEvent.RoundEndPlayerInfo playerInfo)
        {
            // Player Sprite column
            if (playerInfo.PlayerNetEntity != null)
            {
                _playerGrid.AddChild(new SpriteView(playerInfo.PlayerNetEntity.Value, _entityManager)
                {
                    OverrideDirection = Direction.South,
                    VerticalAlignment = VAlignment.Center,
                    SetSize = new Vector2(32, 32),
                });
            }
            else
            {
                _playerGrid.AddChild(new Control
                {
                    SetSize = new Vector2(32, 32),
                });
            }

            // IC Name column  
            var icNameLabel = new Label
            {
                Text = playerInfo.PlayerICName ?? playerInfo.PlayerOOCName,
                VerticalAlignment = VAlignment.Center,
                HorizontalExpand = true,
                ClipText = true
            };

            // Apply color coding for antagonists
            if (playerInfo.Antag)
            {
                icNameLabel.FontColorOverride = Color.Red;
            }

            _playerGrid.AddChild(icNameLabel);

            // Job Icon column
            var jobIconTexture = GetJobIconTexture(playerInfo);
            if (jobIconTexture != null)
            {
                _playerGrid.AddChild(new TextureRect
                {
                    Texture = jobIconTexture,
                    TextureScale = new Vector2(2, 2f),
                    Stretch = TextureRect.StretchMode.KeepCentered,
                    VerticalAlignment = VAlignment.Center,
                });
            }
            else
            {
                _playerGrid.AddChild(new Control
                {
                    SetSize = new Vector2(32, 32),
                });
            }

            // Role column
            var roleLabel = new Label
            {
                Text = playerInfo.Observer ? "-" : Loc.GetString(playerInfo.Role),
                VerticalAlignment = VAlignment.Center,
                HorizontalExpand = true,
                ClipText = true
            };
            _playerGrid.AddChild(roleLabel);

            // Player Type column
            var playerTypeLabel = new Label
            {
                Text = GetPlayerTypeText(playerInfo),
                VerticalAlignment = VAlignment.Center,
                HorizontalExpand = true,
                ClipText = true
            };

            // Apply color coding based on player type
            if (playerInfo.Antag)
            {
                playerTypeLabel.FontColorOverride = Color.Red;
            }
            else if (playerInfo.Observer)
            {
                playerTypeLabel.FontColorOverride = Color.Gray;
            }

            _playerGrid.AddChild(playerTypeLabel);

            // OOC Name column
            var oocNameLabel = new Label
            {
                Text = playerInfo.PlayerOOCName,
                VerticalAlignment = VAlignment.Center,
                HorizontalExpand = true,
                ClipText = true
            };

            _playerGrid.AddChild(oocNameLabel);
        }

        /// <summary>
        /// Gets the player type text for a player based on their observer and antagonist flags
        /// </summary>
        private static string GetPlayerTypeText(RoundEndMessageEvent.RoundEndPlayerInfo playerInfo)
        {
            if (playerInfo.Observer)
                return Loc.GetString("round-end-summary-window-player-manifest-tab-sort-player-type-observer");
            if (playerInfo.Antag)
                return Loc.GetString("round-end-summary-window-player-manifest-tab-sort-player-type-antag");

            return Loc.GetString("round-end-summary-window-player-manifest-tab-sort-player-type-crew");
        }

        /// <summary>
        /// Gets a sort key for player type to ensure consistent ordering: Antagonist -> Crew -> Observer
        /// </summary>
        private static int GetPlayerTypeSortKey(RoundEndMessageEvent.RoundEndPlayerInfo playerInfo)
        {
            if (playerInfo.Antag)
                return 1; // Antagonists first
            if (playerInfo.Observer)
                return 3; // Observers last
            return 2; // Crew in middle
        }

        /// <summary>
        /// Checks if a player matches the current search filter
        /// </summary>
        private bool PlayerMatchesSearch(RoundEndMessageEvent.RoundEndPlayerInfo playerInfo)
        {
            if (string.IsNullOrEmpty(_searchText))
                return true;

            // Search in character name (IC name)
            if (!string.IsNullOrEmpty(playerInfo.PlayerICName) &&
                playerInfo.PlayerICName.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                return true;

            // Search in player name (OOC name)
            if (!string.IsNullOrEmpty(playerInfo.PlayerOOCName) &&
                playerInfo.PlayerOOCName.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                return true;

            // Search in role
            if (!string.IsNullOrEmpty(playerInfo.Role))
            {
                if (playerInfo.Role.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    Loc.GetString(playerInfo.Role).Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // Search in player type
            var playerType = GetPlayerTypeText(playerInfo);
            if (playerType.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                return true;

            // Search for "Observer" when they are observers
            if (playerInfo.Observer && "observer".Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Handles search text changes and refreshes the player list
        /// </summary>
        private void OnSearchTextChanged(LineEdit.LineEditEventArgs args)
        {
            _searchText = args.Text;
            RefreshPlayerList();
        }

        /// <summary>
        /// Gets the job icon texture for a player
        /// </summary>
        private Texture? GetJobIconTexture(RoundEndMessageEvent.RoundEndPlayerInfo playerInfo)
        {
            Texture? texture = null;
            try
            {
                if (playerInfo.Observer)
                {
                    if (_prototypeManager.TryIndex(ObserverIcon, out var observerIcon))
                        texture = _spriteSystem.Frame0(observerIcon.Icon);
                    else if (_prototypeManager.TryIndex(UnknownIcon, out var unknownIcon))
                        texture = _spriteSystem.Frame0(unknownIcon.Icon);
                }
                else
                {
                    var jobId = playerInfo.JobPrototypes?.FirstOrDefault() ?? playerInfo.Role;
                    if (_prototypeManager.TryIndex<JobPrototype>(jobId, out var jobProto))
                    {
                        if (_prototypeManager.TryIndex(jobProto.Icon, out var jobIcon))
                            texture = _spriteSystem.Frame0(jobIcon.Icon);
                    }

                    if (texture == null && _prototypeManager.TryIndex(UnknownIcon, out var fallbackIcon))
                    {
                        texture = _spriteSystem.Frame0(fallbackIcon.Icon);
                    }
                }
            }
            catch (Exception e)
            {
                _sawmill.Warning($"Failed to get job icon for role '{playerInfo.Role}': {e.Message}");
            }

            return texture;
        }

        private sealed class SortButton : Button
        {
            public SortField Field { get; }
            private readonly Label _label;
            private readonly Label _sortIndicator;

            public SortButton(string text, SortField field)
            {
                Field = field;
                HorizontalExpand = true;

                var container = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    HorizontalExpand = true
                };

                _label = new Label
                {
                    Text = text,
                    HorizontalExpand = true
                };

                _sortIndicator = new Label
                {
                    Text = "",
                    HorizontalAlignment = HAlignment.Right,
                    MinSize = new Vector2(15, 1)
                };

                container.AddChild(_label);
                container.AddChild(_sortIndicator);

                AddChild(container);
            }

            public void SetSortIndicator(bool active, bool descending = false)
            {
                if (!active)
                {
                    _sortIndicator.Text = "";
                    return;
                }

                _sortIndicator.Text = descending ? "▼" : "▲";
            }
        }
    }
}
