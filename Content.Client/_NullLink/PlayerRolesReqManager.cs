using Content.Shared._NullLink;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Administration.Managers;

public sealed class PlayerRolesReqManager : SharedPlayerRolesReqManager
{
    [Dependency] private readonly INullLinkPlayerRolesManager _playerRolesManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override bool IsAllRolesAvailable(EntityUid uid) 
        => _player.LocalEntity == uid 
        && AllRoles is not null 
        && _playerRolesManager.ContainsAny(AllRoles.Roles);

    public override bool IsAllRolesAvailable(ICommonSession session)
        => _player.LocalSession == session
        && AllRoles is not null
        && _playerRolesManager.ContainsAny(AllRoles.Roles);
}
