using Content.Client.Interfaces;
using Content.Shared.Preferences;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Content.Client
{
    /// <summary>
    /// Receives <see cref="PlayerPreferences"/> and <see cref="GameSettings"/> from the server during the initial connection.
    /// Can send <see cref="PlayerPreferences"/> to store them on the server.
    /// </summary>
    public class ClientPreferencesManager : SharedPreferencesManager, IClientPreferencesManager
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _netManager;
#pragma warning restore 649
        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgPreferencesAndSettings>(nameof(MsgPreferencesAndSettings), HandlePreferencesAndSettings);
        }

        public GameSettings Settings { get; private set; }
        public PlayerPreferences Preferences { get; private set; }

        public void SavePreferences()
        {
            var msg = _netManager.CreateNetMessage<MsgPreferences>();
            msg.Preferences = Preferences;
            _netManager.ClientSendMessage(msg);
        }

        private void HandlePreferencesAndSettings(MsgPreferencesAndSettings message)
        {
            Preferences = message.Preferences;
            Settings = message.Settings;
        }
    }
}
