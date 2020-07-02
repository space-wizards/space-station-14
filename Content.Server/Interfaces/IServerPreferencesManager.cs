using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Preferences;
using Robust.Server.Interfaces.Player;

namespace Content.Server.Interfaces
{
    public interface IServerPreferencesManager
    {
        void FinishInit();
        void OnClientConnected(IPlayerSession session);
        Task<PlayerPreferences> GetPreferencesAsync(string username);
        Task<IEnumerable<KeyValuePair<string, ICharacterProfile>>> GetSelectedProfilesForPlayersAsync(List<string> usernames);
        void StartInit();
    }
}
