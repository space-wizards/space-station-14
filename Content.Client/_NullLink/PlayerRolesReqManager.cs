using Content.Client.Administration.Managers;
using Content.Shared._NullLink;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._NullLink;

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

    public override bool IsAnyRole(ICommonSession session, ulong[] roles)
        => _player.LocalSession == session
        && _playerRolesManager.ContainsAny(roles);

    public override bool IsMentor(EntityUid uid)
        => _player.LocalEntity == uid
        && _mentorReq is not null
        && _playerRolesManager.ContainsAny(_mentorReq.Roles);

    public override bool IsMentor(ICommonSession session)
        => _player.LocalSession == session
        && _mentorReq is not null
        && _playerRolesManager.ContainsAny(_mentorReq.Roles);
        
    public override bool IsPeacefulBypass(EntityUid uid)
        => _player.LocalEntity == uid
        && _peacefulBypass is not null
        && _playerRolesManager.ContainsAny(_peacefulBypass.Roles);
}
