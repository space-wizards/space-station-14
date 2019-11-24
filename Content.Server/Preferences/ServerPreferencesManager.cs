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
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Server.Preferences
{
    /// <summary>
    /// Sends <see cref="SharedPreferencesManager.MsgPreferencesAndSettings"/> before the client joins the lobby.
    /// Receives <see cref="SharedPreferencesManager.MsgPreferences"/> at any time.
    /// </summary>
    public class ServerPreferencesManager : SharedPreferencesManager, IServerPreferencesManager, IPostInjectInit
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNetManager _netManager;
        [Dependency] private readonly IConfigurationManager _configuration;
        [Dependency] private readonly IResourceManager _resourceManager;
#pragma warning restore 649

        private const string PrefsPath = "/preferences/";
        private static readonly ResourcePath PrefsDirPath = new ResourcePath(PrefsPath).ToRootedPath();

        public void PostInject()
        {
            _configuration.RegisterCVar("game.maxcharacterslots", 10);
            _netManager.RegisterNetMessage<MsgPreferences>(nameof(MsgPreferences), HandlePreferencesMessage);
        }

        private void HandlePreferencesMessage(MsgPreferences message)
        {
            SavePreferences(message.Preferences, message.MsgChannel.SessionId.Username);
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
        private PlayerPreferences GetFromYaml(string username)
        {
            var filePath = PrefsDirPath / (username + ".yml");
            if (!_resourceManager.UserData.Exists(filePath))
            {
                return null;
            }

            using (var file = _resourceManager.UserData.Open(filePath, FileMode.Open))
            {
                using (var reader = new StreamReader(file))
                {
                    var serializer = YamlObjectSerializer.NewReader(new YamlMappingNode());
                    var yaml = new YamlStream();
                    yaml.Load(reader);
                    if (yaml.Documents.Count == 0)
                    {
                        return null;
                    }

                    return (PlayerPreferences) serializer.NodeToType(typeof(PlayerPreferences),
                        yaml.Documents[0].RootNode);
                }
            }
        }

        // TODO: Check for characters that can't be in file names
        // Might not be necessary depending on how we do authentication
        private static bool IsValidUsername(string username)
        {
            return true;
        }

        /// <summary>
        /// Retrieves preferences for the given username from storage.
        /// Returns null if the username is invalid.
        /// Creates and saves default preferences if they are not otherwise found, then returns them.
        /// </summary>
        public PlayerPreferences GetPreferences(string username)
        {
            if (!IsValidUsername(username))
            {
                return null;
            }

            var prefs = GetFromYaml(username);
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
                    Gender = Gender.Male,
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
            if (!IsValidPlayerPreferences(prefs))
            {
                return;
            }

            _resourceManager.UserData.CreateDir(PrefsDirPath);
            var filePath = PrefsDirPath / (username + ".yml");
            using (var file = _resourceManager.UserData.Open(filePath, FileMode.Create))
            {
                using (var writer = new StreamWriter(file))
                {
                    var serializer = YamlObjectSerializer.NewWriter(new YamlMappingNode());
                    var serialized = serializer.TypeToNode(prefs);
                    var yaml = new YamlStream(new YamlDocument(serialized));
                    yaml.Save(new YamlMappingFix(new Emitter(writer)), false);
                }
            }
        }
    }
}
