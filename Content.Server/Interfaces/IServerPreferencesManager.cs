using Content.Shared.Preferences;
using Robust.Server.Interfaces.Player;

namespace Content.Server.Interfaces
{
    public interface IServerPreferencesManager
    {
        void FinishInit();
        void OnClientConnected(IPlayerSession session);
        PlayerPreferences GetPreferences(string username);
        void StartInit();
    }
}
