using Content.Client.Interfaces;
using Content.Shared.Preferences;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Content.Client
{
    /// <summary>
    /// Receives <see cref="PlayerPreferences"/> and <see cref="GameSettings"/> from the server during the initial connection.
    /// Stores preferences on the server through <see cref="SelectCharacter"/> and <see cref="UpdateCharacter"/>.
    /// </summary>
    public class ClientPreferencesManager : SharedPreferencesManager, IClientPreferencesManager
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _netManager;
#pragma warning restore 649

        public GameSettings Settings { get; private set; }
        public PlayerPreferences Preferences { get; private set; }

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgPreferencesAndSettings>(nameof(MsgPreferencesAndSettings),
                HandlePreferencesAndSettings);
        }

        private void HandlePreferencesAndSettings(MsgPreferencesAndSettings message)
        {
            Preferences = message.Preferences;
            Settings = message.Settings;
        }

        public void SelectCharacter(int slot)
        {
            var msg = _netManager.CreateNetMessage<MsgSelectCharacter>();
            msg.SelectedCharacterIndex = slot;
            _netManager.ClientSendMessage(msg);
        }

        public void UpdateCharacter(ICharacterProfile profile, int slot)
        {
            var msg = _netManager.CreateNetMessage<MsgUpdateCharacter>();
            msg.Profile = profile;
            msg.Slot = slot;
            _netManager.ClientSendMessage(msg);
        }
    }
}
