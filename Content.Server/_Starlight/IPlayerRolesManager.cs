using Content.Shared.Starlight;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Managers;

public interface IPlayerRolesManager : ISharedPlayersRoleManager
{
    IEnumerable<ICommonSession> Mentors { get; }

    void Initialize();
}
