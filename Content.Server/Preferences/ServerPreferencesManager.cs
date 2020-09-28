using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.Interfaces;
using Content.Shared;
using Content.Shared.Preferences;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

#nullable enable

namespace Content.Server.Preferences
{
    /// <summary>
    /// Sends <see cref="SharedPreferencesManager.MsgPreferencesAndSettings"/> before the client joins the lobby.
    /// Receives <see cref="SharedPreferencesManager.MsgSelectCharacter"/> and <see cref="SharedPreferencesManager.MsgUpdateCharacter"/> at any time.
    /// </summary>
    public class ServerPreferencesManager : SharedPreferencesManager, IServerPreferencesManager
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IPrototypeManager _protos = default!;

        // Cache player prefs on the server so we don't need as much async hell related to them.
        private readonly Dictionary<NetUserId, PlayerPrefData> _cachedPlayerPrefs =
            new Dictionary<NetUserId, PlayerPrefData>();

        private int MaxCharacterSlots => _cfg.GetCVar(CCVars.GameMaxCharacterSlots);

        public void Init()
        {
            _netManager.RegisterNetMessage<MsgPreferencesAndSettings>(nameof(MsgPreferencesAndSettings));
            _netManager.RegisterNetMessage<MsgSelectCharacter>(nameof(MsgSelectCharacter),
                HandleSelectCharacterMessage);
            _netManager.RegisterNetMessage<MsgUpdateCharacter>(nameof(MsgUpdateCharacter),
                HandleUpdateCharacterMessage);
        }


        private async void HandleSelectCharacterMessage(MsgSelectCharacter message)
        {
            var index = message.SelectedCharacterIndex;
            var userId = message.MsgChannel.UserId;

            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded.IsCompleted)
            {
                Logger.WarningS("prefs", $"User {userId} tried to modify preferences before they loaded.");
                return;
            }

            if (index < 0 || index >= MaxCharacterSlots)
            {
                return;
            }

            var curPrefs = prefsData.Prefs!;

            prefsData.Prefs = new PlayerPreferences(curPrefs.Characters, index);

            if (ShouldStorePrefs(message.MsgChannel.AuthType))
            {
                await _db.SaveSelectedCharacterIndexAsync(message.MsgChannel.UserId, message.SelectedCharacterIndex);
            }
        }

        private async void HandleUpdateCharacterMessage(MsgUpdateCharacter message)
        {
            var slot = message.Slot;
            var profile = message.Profile;
            var userId = message.MsgChannel.UserId;

            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded.IsCompleted)
            {
                Logger.WarningS("prefs", $"User {userId} tried to modify preferences before they loaded.");
                return;
            }

            if (slot < 0 || slot >= MaxCharacterSlots)
            {
                return;
            }

            var curPrefs = prefsData.Prefs!;

            var arr = new ICharacterProfile[MaxCharacterSlots];
            curPrefs.Characters.ToList().CopyTo(arr, 0);

            arr[slot] = HumanoidCharacterProfile.EnsureValid((HumanoidCharacterProfile) profile, _protos);

            prefsData.Prefs = new PlayerPreferences(arr, slot);

            if (ShouldStorePrefs(message.MsgChannel.AuthType))
            {
                await _db.SaveCharacterSlotAsync(message.MsgChannel.UserId, message.Profile, message.Slot);
            }
        }

        public async void OnClientConnected(IPlayerSession session)
        {
            if (!ShouldStorePrefs(session.ConnectedClient.AuthType))
            {
                // Don't store data for guests.
                var prefsData = new PlayerPrefData
                {
                    PrefsLoaded = Task.CompletedTask,
                    Prefs = new PlayerPreferences(
                        new ICharacterProfile[] {HumanoidCharacterProfile.Default()},
                        0)
                };

                _cachedPlayerPrefs[session.UserId] = prefsData;
            }
            else
            {
                var prefsData = new PlayerPrefData();
                var loadTask = LoadPrefs();
                prefsData.PrefsLoaded = loadTask;
                _cachedPlayerPrefs[session.UserId] = prefsData;

                await loadTask;

                async Task LoadPrefs()
                {
                    var prefs = await GetOrCreatePreferencesAsync(session.UserId);
                    prefsData.Prefs = prefs;

                    var msg = _netManager.CreateNetMessage<MsgPreferencesAndSettings>();
                    msg.Preferences = prefs;
                    msg.Settings = new GameSettings
                    {
                        MaxCharacterSlots = MaxCharacterSlots
                    };
                    _netManager.ServerSendMessage(msg, session.ConnectedClient);
                }
            }
        }


        public void OnClientDisconnected(IPlayerSession session)
        {
            _cachedPlayerPrefs.Remove(session.UserId);
        }

        public bool HavePreferencesLoaded(IPlayerSession session)
        {
            return _cachedPlayerPrefs.ContainsKey(session.UserId);
        }

        public Task WaitPreferencesLoaded(IPlayerSession session)
        {
            return _cachedPlayerPrefs[session.UserId].PrefsLoaded;
        }

        /// <summary>
        /// Retrieves preferences for the given username from storage.
        /// Creates and saves default preferences if they are not found, then returns them.
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

        private async Task<PlayerPreferences> GetOrCreatePreferencesAsync(NetUserId userId)
        {
            var prefs = await _db.GetPlayerPreferencesAsync(userId);
            if (prefs is null)
            {
                return await _db.InitPrefsAsync(userId, HumanoidCharacterProfile.Default());
            }

            return prefs;
        }

        public IEnumerable<KeyValuePair<NetUserId, ICharacterProfile>> GetSelectedProfilesForPlayers(
            List<NetUserId> usernames)
        {
            return usernames
                .Select(p => (_cachedPlayerPrefs[p].Prefs, p))
                .Where(p => p.Prefs != null)
                .Select(p =>
                {
                    var idx = p.Prefs!.SelectedCharacterIndex;
                    return new KeyValuePair<NetUserId, ICharacterProfile>(p.p, p.Prefs!.GetProfile(idx));
                });
        }

        internal static bool ShouldStorePrefs(LoginType loginType)
        {
            return loginType.HasStaticUserId();
        }

        private sealed class PlayerPrefData
        {
            public Task PrefsLoaded = default!;
            public PlayerPreferences? Prefs;
        }
    }
}
