using System.Collections.Generic;
using System.IO;
using Content.Server.Interfaces;
using Content.Shared.Preferences;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Server.Preferences
{
    /// <summary>
    /// Sends <see cref="SharedPreferencesManager.MsgPreferencesAndSettings"/> before the client joins the lobby.
    /// Receives <see cref="SharedPreferencesManager.MsgPreferences"/> at any time.
    /// </summary>
    public class ServerPreferencesManager : SharedPreferencesManager, IServerPreferencesManager
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNetManager _netManager;
        [Dependency] private readonly IConfigurationManager _configuration;
        [Dependency] private readonly IResourceManager _resourceManager;
#pragma warning restore 649
        private PreferencesDatabase _preferencesDb;

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgPreferencesAndSettings>(nameof(MsgPreferencesAndSettings));
            _netManager.RegisterNetMessage<MsgSelectCharacter>(nameof(MsgSelectCharacter),
                HandleSelectCharacterMessage);
            _netManager.RegisterNetMessage<MsgUpdateCharacter>(nameof(MsgUpdateCharacter),
                HandleUpdateCharacterMessage);

            _configuration.RegisterCVar("game.maxcharacterslots", 10);
            _configuration.RegisterCVar("game.preferencesdbpath", "preferences.db");

            var configPreferencesDbPath = _configuration.GetCVar<string>("game.preferencesdbpath");
            var finalPreferencesDbPath = Path.Combine(_resourceManager.UserData.RootDir, configPreferencesDbPath);

            var maxCharacterSlots = _configuration.GetCVar<int>("game.maxcharacterslots");

            _preferencesDb = new PreferencesDatabase(finalPreferencesDbPath, maxCharacterSlots);
        }

        private void HandleSelectCharacterMessage(MsgSelectCharacter message)
        {
            _preferencesDb.SaveSelectedCharacterIndex(message.MsgChannel.SessionId.Username, message.SelectedCharacterIndex);
        }

        private void HandleUpdateCharacterMessage(MsgUpdateCharacter message)
        {
            _preferencesDb.SaveCharacterSlot(message.MsgChannel.SessionId.Username, message.Profile, message.Slot);
        }

        public void OnClientConnected(IPlayerSession session)
        {
            var msg = _netManager.CreateNetMessage<MsgPreferencesAndSettings>();
            msg.Preferences = GetPreferences(session.SessionId.Username);
            msg.Settings = new GameSettings
            {
                MaxCharacterSlots = _configuration.GetCVar<int>("game.maxcharacterslots")
            };
            _netManager.ServerSendMessage(msg, session.ConnectedClient);
        }

        /// <summary>
        /// Returns the requested <see cref="PlayerPreferences"/> or null if not found.
        /// </summary>
        private PlayerPreferences GetFromSql(string username)
        {
            return _preferencesDb.GetPlayerPreferences(username);
        }

        /// <summary>
        /// Retrieves preferences for the given username from storage.
        /// Creates and saves default preferences if they are not otherwise found, then returns them.
        /// </summary>
        public PlayerPreferences GetPreferences(string username)
        {
            var prefs = GetFromSql(username);
            if (prefs is null)
            {
                prefs = DefaultPlayerPreferences();
                SavePreferences(prefs, username);
            }

            return prefs;
        }

        private PlayerPreferences DefaultPlayerPreferences() // TODO: Create random character instead
        {
            var prefs = new PlayerPreferences();
            prefs.Characters = new List<ICharacterProfile>
            {
                new HumanoidCharacterProfile
                {
                    Age = 18,
                    CharacterAppearance = new HumanoidCharacterAppearance
                    {
                        EyeColor = Color.Green,
                        FacialHairColor = Color.Black,
                        HairColor = Color.White
                    },
                    Sex = Sex.Male,
                    Name = "John Doe"
                }
            };
            prefs.SelectedCharacterIndex = 0;
            return prefs;
        }

        private bool IsValidPlayerPreferences(PlayerPreferences prefs)
        {
            var configMaxCharacters = _configuration.GetCVar<int>("game.maxcharacterslots");
            if (prefs.Characters is null)
                return false;
            if (prefs.SelectedCharacterIndex > configMaxCharacters)
                return false;
            if (prefs.SelectedCharacterIndex >= prefs.Characters.Count)
                return false;
            return true;
        }

        /// <summary>
        /// Saves the given preferences to storage.
        /// </summary>
        public void SavePreferences(PlayerPreferences prefs, string username)
        {
            _preferencesDb.SaveSelectedCharacterIndex(username, prefs.SelectedCharacterIndex);
            var index = 0;
            foreach (var character in prefs.Characters)
            {
                _preferencesDb.SaveCharacterSlot(username, character, index);
                index++;
            }
        }
    }
}
