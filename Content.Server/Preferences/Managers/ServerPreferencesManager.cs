using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Construction.Prototypes;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Preferences.Managers
{
    /// <summary>
    /// Sends <see cref="MsgPreferencesAndSettings"/> before the client joins the lobby.
    /// Receives <see cref="MsgSetCharacterEnable"/>, <see cref="MsgUpdateCharacter"/>,
    /// <see cref="MsgDeleteCharacter"/>, and <see cref="MsgUpdateJobPriorities"/> at any time.
    /// </summary>
    public sealed class ServerPreferencesManager : IServerPreferencesManager, IPostInjectInit
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IDependencyCollection _dependencies = default!;
        [Dependency] private readonly ILogManager _log = default!;
        [Dependency] private readonly UserDbDataManager _userDb = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        // Cache player prefs on the server so we don't need as much async hell related to them.
        private readonly Dictionary<NetUserId, PlayerPrefData> _cachedPlayerPrefs =
            new();

        private ISawmill _sawmill = default!;

        private int MaxCharacterSlots => _cfg.GetCVar(CCVars.GameMaxCharacterSlots);

        public void Init()
        {
            _netManager.RegisterNetMessage<MsgPreferencesAndSettings>();
            _netManager.RegisterNetMessage<MsgUpdateCharacter>(HandleUpdateCharacterMessage);
            _netManager.RegisterNetMessage<MsgDeleteCharacter>(HandleDeleteCharacterMessage);
            _netManager.RegisterNetMessage<MsgUpdateConstructionFavorites>(HandleUpdateConstructionFavoritesMessage);
            _netManager.RegisterNetMessage<MsgSetCharacterEnable>(HandleSetCharacterEnableMessage);
            _netManager.RegisterNetMessage<MsgUpdateJobPriorities>(HandleUpdateJobPrioritiesMessage);
            _sawmill = _log.GetSawmill("prefs");
        }

        private async void HandleUpdateCharacterMessage(MsgUpdateCharacter message)
        {
            var userId = message.MsgChannel.UserId;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (message.Profile == null)
                _sawmill.Error($"User {userId} sent a {nameof(MsgUpdateCharacter)} with a null profile in slot {message.Slot}.");
            else
                await SetProfile(userId, message.Slot, message.Profile);
        }

        public async Task SetProfile(NetUserId userId, int slot, ICharacterProfile profile)
        {
            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Error($"Tried to modify user {userId} preferences before they loaded.");
                return;
            }

            if (slot < 0 || slot >= MaxCharacterSlots)
                return;

            var curPrefs = prefsData.Prefs!;
            var session = _playerManager.GetSessionById(userId);

            profile.EnsureValid(session, _dependencies);

            var profiles = new Dictionary<int, ICharacterProfile>(curPrefs.Characters)
            {
                [slot] = profile
            };

            prefsData.Prefs = new PlayerPreferences(profiles, curPrefs.AdminOOCColor, curPrefs.ConstructionFavorites, curPrefs.JobPriorities);

            if (ShouldStorePrefs(session.Channel.AuthType))
                await _db.SaveCharacterSlotAsync(userId, profile, slot);
        }

        public async Task SetConstructionFavorites(NetUserId userId, List<ProtoId<ConstructionPrototype>> favorites)
        {
            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Error($"Tried to modify user {userId} preferences before they loaded.");
                return;
            }

            var curPrefs = prefsData.Prefs!;
            prefsData.Prefs = new PlayerPreferences(curPrefs.Characters, curPrefs.AdminOOCColor, favorites, curPrefs.JobPriorities);

            var session = _playerManager.GetSessionById(userId);
            if (ShouldStorePrefs(session.Channel.AuthType))
                await _db.SaveConstructionFavoritesAsync(userId, favorites);
        }

        /// <summary>
        /// Update the job priorities dictionary for a given player
        /// </summary>
        public async Task SetJobPriorities(NetUserId userId, Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
        {
            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Warning("prefs", $"User {userId} tried to modify preferences before they loaded.");
                return;
            }

            var curPrefs = prefsData.Prefs!;
            var session = _playerManager.GetSessionById(userId);

            prefsData.Prefs = new PlayerPreferences(curPrefs.Characters, curPrefs.AdminOOCColor, curPrefs.ConstructionFavorites, jobPriorities);

            if (ShouldStorePrefs(session.Channel.AuthType))
                await _db.SaveJobPrioritiesAsync(userId, jobPriorities);
        }

        private async void HandleDeleteCharacterMessage(MsgDeleteCharacter message)
        {
            var slot = message.Slot;
            var userId = message.MsgChannel.UserId;

            await DeleteProfile(userId, slot);
        }

        /// <summary>
        /// Delete a character profile for the given player in the given slot
        /// </summary>
        public async Task DeleteProfile(NetUserId userId, int slot)
        {
            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Warning($"User {userId} tried to modify preferences before they loaded.");
                return;
            }

            if (slot < 0 || slot >= MaxCharacterSlots)
            {
                return;
            }

            var curPrefs = prefsData.Prefs!;
            var session = _playerManager.GetSessionById(userId);

            var arr = new Dictionary<int, ICharacterProfile>(curPrefs.Characters);
            arr.Remove(slot);

            prefsData.Prefs = new PlayerPreferences(arr, curPrefs.AdminOOCColor, curPrefs.ConstructionFavorites, curPrefs.JobPriorities);

            if (ShouldStorePrefs(session.AuthType))
            {
                await _db.SaveCharacterSlotAsync(userId, null, slot);
            }
        }

        /// <summary>
        /// Handle the net message from a client to enable or disable a character in a given slot
        /// </summary>
        private async void HandleSetCharacterEnableMessage(MsgSetCharacterEnable message)
        {
            var slot = message.CharacterIndex;
            var val = message.EnabledValue;

            var userId = message.MsgChannel.UserId;

            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Warning("prefs", $"User {userId} tried to modify preferences before they loaded.");
                return;
            }

            var curPrefs = prefsData.Prefs!;
            var session = _playerManager.GetSessionById(userId);

            if (!curPrefs.Characters.TryGetValue(slot, out var characterProfile))
            {
                // Non-existent slot.
                return;
            }

            if (characterProfile is not HumanoidCharacterProfile profile)
                return;

            profile.Enabled = val;
            var profiles = new Dictionary<int, ICharacterProfile>(curPrefs.Characters)
            {
                [slot] = new HumanoidCharacterProfile(profile),
            };

            prefsData.Prefs = new PlayerPreferences(profiles, curPrefs.AdminOOCColor, curPrefs.ConstructionFavorites, curPrefs.JobPriorities);

            if (ShouldStorePrefs(session.Channel.AuthType))
                await _db.SaveCharacterSlotAsync(userId, profile, slot);
        }

        /// <summary>
        /// Handler for the message from a client to update a player's job priorities dictionary
        /// </summary>
        public async void HandleUpdateJobPrioritiesMessage(MsgUpdateJobPriorities message)
        {
            var userId = message.MsgChannel.UserId;

            await SetJobPriorities(userId, message.JobPriorities);
        }

        private async void HandleUpdateConstructionFavoritesMessage(MsgUpdateConstructionFavorites message)
        {
            var userId = message.MsgChannel.UserId;
            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Warning($"User {userId} tried to modify preferences before they loaded.");
                return;
            }

            // Validate items in the message so that a modified client cannot freely store a gigabyte of arbitrary data.
            var validatedSet = new HashSet<ProtoId<ConstructionPrototype>>();
            foreach (var favorite in message.Favorites)
            {
                if (_prototypeManager.HasIndex(favorite))
                    validatedSet.Add(favorite);
            }

            var validatedList = message.Favorites;
            if (validatedSet.Count != message.Favorites.Count)
            {
                // A difference in counts indicates that unrecognized or duplicate IDs are present.
                _sawmill.Warning($"User {userId} sent invalid construction favorites.");
                validatedList = validatedSet.ToList();
            }

            var curPrefs = prefsData.Prefs!;
            prefsData.Prefs = new PlayerPreferences(curPrefs.Characters, curPrefs.AdminOOCColor, validatedList, curPrefs.JobPriorities);

            if (ShouldStorePrefs(message.MsgChannel.AuthType))
            {
                await _db.SaveConstructionFavoritesAsync(userId, validatedList);
            }
        }

        // Should only be called via UserDbDataManager.
        public async Task LoadData(ICommonSession session, CancellationToken cancel)
        {
            if (!ShouldStorePrefs(session.Channel.AuthType))
            {
                // Don't store data for guests.
                var prefsData = new PlayerPrefData
                {
                    PrefsLoaded = true,
                    Prefs = new PlayerPreferences(
                        new[] {new KeyValuePair<int, ICharacterProfile>(0, HumanoidCharacterProfile.Random())},
                        Color.Transparent,
                        [],
                        new Dictionary<ProtoId<JobPrototype>, JobPriority>{{ SharedGameTicker.FallbackOverflowJob, JobPriority.High }}),
                };

                _cachedPlayerPrefs[session.UserId] = prefsData;
            }
            else
            {
                var prefsData = new PlayerPrefData();
                var loadTask = LoadPrefs();
                _cachedPlayerPrefs[session.UserId] = prefsData;

                await loadTask;

                async Task LoadPrefs()
                {
                    var prefs = await GetOrCreatePreferencesAsync(session.UserId, cancel);
                    prefsData.Prefs = prefs;
                }
            }
        }

        public void FinishLoad(ICommonSession session)
        {
            // This is a separate step from the actual database load.
            // Sanitizing preferences requires play time info due to loadouts.
            // And play time info is loaded concurrently from the DB with preferences.
            var prefsData = _cachedPlayerPrefs[session.UserId];
            DebugTools.Assert(prefsData.Prefs != null);
            prefsData.Prefs = SanitizePreferences(session, prefsData.Prefs, _dependencies);

            prefsData.PrefsLoaded = true;

            var msg = new MsgPreferencesAndSettings();
            msg.Preferences = prefsData.Prefs;
            msg.Settings = new GameSettings
            {
                MaxCharacterSlots = MaxCharacterSlots
            };
            _netManager.ServerSendMessage(msg, session.Channel);
        }

        public void OnClientDisconnected(ICommonSession session)
        {
            _cachedPlayerPrefs.Remove(session.UserId);
        }

        public bool HavePreferencesLoaded(ICommonSession session)
        {
            return _cachedPlayerPrefs.ContainsKey(session.UserId);
        }


        /// <summary>
        /// Tries to get the preferences from the cache
        /// </summary>
        /// <param name="userId">User Id to get preferences for</param>
        /// <param name="playerPreferences">The user preferences if true, otherwise null</param>
        /// <returns>If preferences are not null</returns>
        public bool TryGetCachedPreferences(NetUserId userId,
            [NotNullWhen(true)] out PlayerPreferences? playerPreferences)
        {
            if (_cachedPlayerPrefs.TryGetValue(userId, out var prefs))
            {
                playerPreferences = prefs.Prefs;
                return prefs.Prefs != null;
            }

            playerPreferences = null;
            return false;
        }

        /// <summary>
        /// Retrieves preferences for the given username from storage.
        /// </summary>
        public PlayerPreferences GetPreferences(NetUserId userId)
        {
            var prefs = _cachedPlayerPrefs[userId].Prefs;
            if (prefs == null)
            {
                throw new InvalidOperationException("Preferences for this player have not loaded yet.");
            }

            return prefs;
        }

        /// <summary>
        /// Retrieves preferences for the given username from storage or returns null.
        /// </summary>
        public PlayerPreferences? GetPreferencesOrNull(NetUserId? userId)
        {
            if (userId == null)
                return null;

            if (_cachedPlayerPrefs.TryGetValue(userId.Value, out var pref))
                return pref.Prefs;
            return null;
        }

        private async Task<PlayerPreferences> GetOrCreatePreferencesAsync(NetUserId userId, CancellationToken cancel)
        {
            var prefs = await _db.GetPlayerPreferencesAsync(userId, cancel);
            if (prefs is null)
            {
                return await _db.InitPrefsAsync(userId, HumanoidCharacterProfile.Random().AsEnabled(), cancel);
            }

            return prefs;
        }

        private PlayerPreferences SanitizePreferences(ICommonSession session, PlayerPreferences prefs, IDependencyCollection collection)
        {
            // Clean up preferences in case of changes to the game,
            // such as removed jobs still being selected.
            var prototypeManager = collection.Resolve<IPrototypeManager>();

            // Sanitize the job priorities
            var priorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>(prefs.JobPriorities
                .Where(p => prototypeManager.TryIndex(p.Key, out var job) && job.SetPreference && p.Value switch
                {
                    JobPriority.Never => false, // Drop never since that's assumed default.
                    JobPriority.Low => true,
                    JobPriority.Medium => true,
                    JobPriority.High => true,
                    _ => false
                }));

            // Ensure only one high priority job
            var hasHighPrio = false;
            foreach (var (key, value) in priorities)
            {
                if (value != JobPriority.High)
                    continue;

                if (hasHighPrio)
                    priorities[key] = JobPriority.Medium;
                hasHighPrio = true;
            }

            return new PlayerPreferences(prefs.Characters.Select(p =>
            {
                return new KeyValuePair<int, ICharacterProfile>(p.Key, p.Value.Validated(session, collection));
            }), prefs.AdminOOCColor, prefs.ConstructionFavorites, priorities);
        }

        internal static bool ShouldStorePrefs(LoginType loginType)
        {
            return loginType.HasStaticUserId();
        }

        private sealed class PlayerPrefData
        {
            public bool PrefsLoaded;
            public PlayerPreferences? Prefs;
        }

        void IPostInjectInit.PostInject()
        {
            _userDb.AddOnLoadPlayer(LoadData);
            _userDb.AddOnFinishLoad(FinishLoad);
            _userDb.AddOnPlayerDisconnect(OnClientDisconnected);
        }
    }
}
