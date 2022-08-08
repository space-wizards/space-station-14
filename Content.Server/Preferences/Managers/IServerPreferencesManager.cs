using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server.Preferences.Managers
{
    public interface IServerPreferencesManager
    {
        void Init();

        Task LoadData(IPlayerSession session, CancellationToken cancel);
        void OnClientDisconnected(IPlayerSession session);

        PlayerPreferences GetPreferences(NetUserId userId);
        IEnumerable<KeyValuePair<NetUserId, ICharacterProfile>> GetSelectedProfilesForPlayers(List<NetUserId> userIds);
    }
}
