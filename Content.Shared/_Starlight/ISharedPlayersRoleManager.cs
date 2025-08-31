using System.Linq;
using Content.Shared.Administration;
using Robust.Shared.Player;

namespace Content.Shared.Starlight;

public interface ISharedPlayersRoleManager
{

    PlayerData? GetPlayerData(EntityUid uid);
    PlayerData? GetPlayerData(ICommonSession session);
}
