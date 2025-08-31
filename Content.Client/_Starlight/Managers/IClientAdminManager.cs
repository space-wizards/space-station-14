using Content.Shared.Starlight;
using Content.Shared.Administration;

namespace Content.Client._Starlight.Managers;

public interface IClientPlayerRolesManager
{
    event Action PlayerStatusUpdated;

    PlayerData? GetPlayerData();

    void Initialize();
}
