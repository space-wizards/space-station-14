using Content.Shared.Preferences;
using Robust.Server.Interfaces.Player;

namespace Content.Server.Interfaces
{
    public interface IServerPreferencesManager
    {
        void OnClientConnected(IPlayerSession session);
        PlayerPreferences GetPreferences(string username);
        void SavePreferences(PlayerPreferences prefs, string username);
    }
}
