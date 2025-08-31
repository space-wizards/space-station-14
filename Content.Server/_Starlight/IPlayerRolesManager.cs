using Content.Shared.Starlight;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using static Content.Server.Starlight.PlayerRolesManager;

namespace Content.Server.Administration.Managers;

public interface IPlayerRolesManager : ISharedPlayersRoleManager
{
    IEnumerable<PlayerReg> Players { get; }

    void Initialize();
}
