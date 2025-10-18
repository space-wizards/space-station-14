using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Preferences.Managers
{
    public interface IServerPreferencesManager
    {
        void Init();

        Task LoadData(ICommonSession session, CancellationToken cancel);
        void FinishLoad(ICommonSession session);
        void OnClientDisconnected(ICommonSession session);

        bool TryGetCachedPreferences(NetUserId userId, [NotNullWhen(true)] out PlayerPreferences? playerPreferences);
        PlayerPreferences GetPreferences(NetUserId userId);
        PlayerPreferences? GetPreferencesOrNull(NetUserId? userId);
        bool HavePreferencesLoaded(ICommonSession session);

        Task SetProfile(NetUserId userId, int slot, ICharacterProfile profile);
        Task SetConstructionFavorites(NetUserId userId, List<ProtoId<ConstructionPrototype>> favorites);

        /// <summary>
        /// Save a player's job priorities to their player profile.
        /// </summary>
        Task SetJobPriorities(NetUserId userId, Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities);

        /// <summary>
        /// Delete the character profile in the given slot from a player's profile
        /// </summary>
        Task DeleteProfile(NetUserId userId, int slot);
    }
}
