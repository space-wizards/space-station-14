using Content.Shared.Starlight;
using Content.Shared.Administration;

namespace Content.Client.Starlight.Managers;

public interface IClientPlayerRolesManager
{
    event Action PlayerStatusUpdated;

    PlayerData? GetPlayerData();

    string? GetDiscordLink();

    bool HasFlag(PlayerFlags flag);

    void Initialize();
}
