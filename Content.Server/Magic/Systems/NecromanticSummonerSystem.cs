using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Magic.Systems;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Magic.Systems;

public sealed class NecromanticSummonerSystem : SharedNecromanticSummonerSystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    // GhostRoleComponent is on the server, so we can't do this in shared.
    public override void SpawnSummonAndTransferPlayer(EntProtoId summonPrototype, EntityCoordinates coords, EntityUid target)
    {
        var spawn = SpawnAtPosition(summonPrototype, coords);
        if (_mind.TryGetMind(target, out var targetMindUid, out var targetMindComp))
        {
            // If the spawned mob has a ghost role, then use that so that the relevant roles get added,
            // otherwise just transfer the mind.
            if (!TryComp<GhostRoleComponent>(spawn, out var ghostRole)
                || !_player.TryGetSessionById(targetMindComp.UserId, out var session)
                || !_ghostRole.Takeover((spawn, ghostRole), session))
                _mind.TransferTo(targetMindUid, spawn, mind: targetMindComp, ghostCheckOverride: true);
        }
    }
}
