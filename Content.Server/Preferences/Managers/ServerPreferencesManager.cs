using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Body;
using Content.Shared.CCVar;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Server.Preferences.Managers
{
    /// <summary>
    /// Sends <see cref="MsgPreferencesAndSettings"/> before the client joins the lobby.
    /// Receives <see cref="MsgSelectCharacter"/> and <see cref="MsgUpdateCharacter"/> at any time.
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
        [Dependency] private readonly MarkingManager _marking = default!;
        [Dependency] private readonly ISerializationManager _serialization = default!;

        // Cache player prefs on the server so we don't need as much async hell related to them.
        private readonly Dictionary<NetUserId, PlayerPrefData> _cachedPlayerPrefs =
            new();

        private ISawmill _sawmill = default!;

        private int MaxCharacterSlots => _cfg.GetCVar(CCVars.GameMaxCharacterSlots);

        public void Init()
        {
            _netManager.RegisterNetMessage<MsgPreferencesAndSettings>();
            _netManager.RegisterNetMessage<MsgSelectCharacter>(HandleSelectCharacterMessage);
            _netManager.RegisterNetMessage<MsgUpdateCharacter>(HandleUpdateCharacterMessage);
            _netManager.RegisterNetMessage<MsgDeleteCharacter>(HandleDeleteCharacterMessage);
            _netManager.RegisterNetMessage<MsgUpdateConstructionFavorites>(HandleUpdateConstructionFavoritesMessage);
            _sawmill = _log.GetSawmill("prefs");
        }

        private static TValue? TryDeserialize<TValue>(JsonDocument document) where TValue : class
        {
            try
            {
                return document.Deserialize<TValue>();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        internal PlayerPreferences ConvertPreferences(Preference prefs)
        {
            var maxSlot = prefs.Profiles.Max(p => p.Slot) + 1;
            var profiles = new Dictionary<int, HumanoidCharacterProfile>(maxSlot);
            foreach (var profile in prefs.Profiles)
            {
                profiles[profile.Slot] = ConvertProfiles(profile);
            }

            var constructionFavorites = new List<ProtoId<ConstructionPrototype>>(prefs.ConstructionFavorites.Count);
            foreach (var favorite in prefs.ConstructionFavorites)
                constructionFavorites.Add(new ProtoId<ConstructionPrototype>(favorite));

            return new PlayerPreferences(profiles, prefs.SelectedCharacterSlot, Color.FromHex(prefs.AdminOOCColor), constructionFavorites);
        }

        internal HumanoidCharacterProfile ConvertProfiles(Profile profile)
        {

            var jobs = profile.Jobs.ToDictionary(j => new ProtoId<JobPrototype>(j.JobName), j => (JobPriority) j.Priority);
            var antags = profile.Antags.Select(a => new ProtoId<AntagPrototype>(a.AntagName));
            var traits = profile.Traits.Select(t => new ProtoId<TraitPrototype>(t.TraitName));

            var sex = Sex.Male;
            if (Enum.TryParse<Sex>(profile.Sex, true, out var sexVal))
                sex = sexVal;

            var spawnPriority = (SpawnPriorityPreference) profile.SpawnPriority;

            var gender = sex == Sex.Male ? Gender.Male : Gender.Female;
            if (Enum.TryParse<Gender>(profile.Gender, true, out var genderVal))
                gender = genderVal;


            var markings =
                new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();

            var species = profile.Species;
            if (!_prototypeManager.HasIndex<SpeciesPrototype>(species))
                species = HumanoidCharacterProfile.DefaultSpecies;

            if (profile.OrganMarkings?.RootElement is { } element)
            {
                var data = element.ToDataNode();
                markings = _serialization
                    .Read<Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>>(
                        data,
                        notNullableOverride: true);
            }
            else if (profile.Markings is { } profileMarkings && TryDeserialize<List<string>>(profileMarkings) is { } markingsRaw)
            {
                List<Marking> markingsList = new();

                foreach (var marking in markingsRaw)
                {
                    var parsed = Marking.ParseFromDbString(marking);

                    if (parsed is null) continue;

                    markingsList.Add(parsed.Value);
                }

                if (Marking.ParseFromDbString($"{profile.HairName}@{profile.HairColor}") is { } facialMarking)
                    markingsList.Add(facialMarking);

                if (Marking.ParseFromDbString($"{profile.HairName}@{profile.HairColor}") is { } hairMarking)
                    markingsList.Add(hairMarking);

                markings = _marking.ConvertMarkings(markingsList, species);
            }

            var loadouts = new Dictionary<string, RoleLoadout>();

            foreach (var role in profile.Loadouts)
            {
                var loadout = new RoleLoadout(role.RoleName)
                {
                    EntityName = role.EntityName,
                };

                foreach (var group in role.Groups)
                {
                    var groupLoadouts = loadout.SelectedLoadouts.GetOrNew(group.GroupName);
                    foreach (var profLoadout in group.Loadouts)
                    {
                        groupLoadouts.Add(new Loadout()
                        {
                            Prototype = profLoadout.LoadoutName,
                        });
                    }
                }

                loadouts[role.RoleName] = loadout;
            }

            return new HumanoidCharacterProfile(
                profile.CharacterName,
                profile.FlavorText,
                species,
                profile.Age,
                sex,
                gender,
                new HumanoidCharacterAppearance
                (
                    Color.FromHex(profile.EyeColor),
                    Color.FromHex(profile.SkinColor),
                    markings
                ),
                spawnPriority,
                jobs,
                (PreferenceUnavailableMode) profile.PreferenceUnavailable,
                antags.ToHashSet(),
                traits.ToHashSet(),
                loadouts
            );
        }

        private async void HandleSelectCharacterMessage(MsgSelectCharacter message)
        {
            var index = message.SelectedCharacterIndex;
            var userId = message.MsgChannel.UserId;

            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Warning($"User {userId} tried to modify preferences before they loaded.");
                return;
            }

            if (index < 0 || index >= MaxCharacterSlots)
            {
                return;
            }

            var curPrefs = prefsData.Prefs!;

            if (!curPrefs.Characters.ContainsKey(index))
            {
                // Non-existent slot.
                return;
            }

            prefsData.Prefs = new PlayerPreferences(curPrefs.Characters, index, curPrefs.AdminOOCColor, curPrefs.ConstructionFavorites);

            if (ShouldStorePrefs(message.MsgChannel.AuthType))
            {
                await _db.SaveSelectedCharacterIndexAsync(message.MsgChannel.UserId, message.SelectedCharacterIndex);
            }
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

        public async Task SetProfile(NetUserId userId, int slot, HumanoidCharacterProfile profile)
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

            var profiles = new Dictionary<int, HumanoidCharacterProfile>(curPrefs.Characters)
            {
                [slot] = profile
            };

            prefsData.Prefs = new PlayerPreferences(profiles, slot, curPrefs.AdminOOCColor, curPrefs.ConstructionFavorites);

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
            prefsData.Prefs = new PlayerPreferences(curPrefs.Characters, curPrefs.SelectedCharacterIndex, curPrefs.AdminOOCColor, favorites);

            var session = _playerManager.GetSessionById(userId);
            if (ShouldStorePrefs(session.Channel.AuthType))
                await _db.SaveConstructionFavoritesAsync(userId, favorites);
        }

        private async void HandleDeleteCharacterMessage(MsgDeleteCharacter message)
        {
            var slot = message.Slot;
            var userId = message.MsgChannel.UserId;

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

            // If they try to delete the slot they have selected then we switch to another one.
            // Of course, that's only if they HAVE another slot.
            int? nextSlot = null;
            if (curPrefs.SelectedCharacterIndex == slot)
            {
                // That ! on the end is because Rider doesn't like .NET 5.
                var (ns, profile) = curPrefs.Characters.FirstOrDefault(p => p.Key != message.Slot)!;
                if (profile == null)
                {
                    // Only slot left, can't delete.
                    return;
                }

                nextSlot = ns;
            }

            var arr = new Dictionary<int, HumanoidCharacterProfile>(curPrefs.Characters);
            arr.Remove(slot);

            prefsData.Prefs = new PlayerPreferences(arr, nextSlot ?? curPrefs.SelectedCharacterIndex, curPrefs.AdminOOCColor, curPrefs.ConstructionFavorites);

            if (ShouldStorePrefs(message.MsgChannel.AuthType))
            {
                if (nextSlot != null)
                {
                    await _db.DeleteSlotAndSetSelectedIndex(userId, slot, nextSlot.Value);
                }
                else
                {
                    await _db.SaveCharacterSlotAsync(userId, null, slot);
                }
            }
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
            prefsData.Prefs = new PlayerPreferences(curPrefs.Characters, curPrefs.SelectedCharacterIndex, curPrefs.AdminOOCColor, validatedList);

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
                        new[] { new KeyValuePair<int, HumanoidCharacterProfile>(0, HumanoidCharacterProfile.Random()) },
                        0, Color.Transparent, [])
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
                    prefsData.Prefs = ConvertPreferences(prefs);
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

        private async Task<Preference> GetOrCreatePreferencesAsync(NetUserId userId, CancellationToken cancel)
        {
            var prefs = await _db.GetPlayerPreferencesAsync(userId, cancel);
            if (prefs is null)
            {
                var speciesToBlacklist =
                    new HashSet<string>(_cfg.GetCVar(CCVars.ICNewAccountSpeciesBlacklist).Split(","));
                return await _db.InitPrefsAsync(userId, HumanoidCharacterProfile.Random(speciesToBlacklist), cancel);
            }

            return prefs;
        }

        private PlayerPreferences SanitizePreferences(ICommonSession session, PlayerPreferences prefs, IDependencyCollection collection)
        {
            // Clean up preferences in case of changes to the game,
            // such as removed jobs still being selected.

            return new PlayerPreferences(prefs.Characters.Select(p =>
            {
                return new KeyValuePair<int, HumanoidCharacterProfile>(p.Key, p.Value.Validated(session, collection));
            }), prefs.SelectedCharacterIndex, prefs.AdminOOCColor, prefs.ConstructionFavorites);
        }

        public IEnumerable<KeyValuePair<NetUserId, HumanoidCharacterProfile>> GetSelectedProfilesForPlayers(
            List<NetUserId> usernames)
        {
            return usernames
                .Select(p => (_cachedPlayerPrefs[p].Prefs, p))
                .Where(p => p.Prefs != null)
                .Select(p => new KeyValuePair<NetUserId, HumanoidCharacterProfile>(p.p, p.Prefs!.SelectedCharacter));
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
