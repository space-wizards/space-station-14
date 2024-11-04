using Content.Shared.Administration;
using Robust.Shared.Player;

namespace Content.Shared._Starlight;

public interface ISharedPlayersRoleManager
{

    PlayerData? GetPlayerData(EntityUid uid);
    PlayerData? GetPlayerData(ICommonSession session);

    bool HasPlayerFlag(EntityUid player, PlayerFlags flag)
    {
        var data = GetPlayerData(player);
        return data != null && data.HasFlag(flag);
    }
    bool HasPlayerFlag(ICommonSession player, PlayerFlags flag)
    {
        var data = GetPlayerData(player);
        return data != null && data.HasFlag(flag);
    }
}
