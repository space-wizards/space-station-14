using System.Linq;
using Content.Shared._NullLink;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._NullLink.PlayerData;

public sealed class PlayerRolesReqManager : SharedPlayerRolesReqManager
{
    [Dependency] private readonly INullLinkPlayerManager _playerManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override bool IsAllRolesAvailable(EntityUid uid) 
        => _player.TryGetSessionByEntity(uid, out var session)
            && AllRoles is not null
            && _playerManager.TryGetPlayerData(session.UserId, out var playerData) 
            && AllRoles.Roles.Any(playerData.Roles.Contains);

    public override bool IsAllRolesAvailable(ICommonSession session)
        =>  AllRoles is not null
            && _playerManager.TryGetPlayerData(session.UserId, out var playerData)
            && AllRoles.Roles.Any(playerData.Roles.Contains);
}