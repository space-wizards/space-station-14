using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.Ghost;
using Content.Shared.Ghost;
using Content.Shared.Voting;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Threading.Tasks;

namespace Content.Server.Voting;

public sealed class VotingSystem : EntitySystem
{

    private EntityQuery<GhostComponent> _ghostQuery;

    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();

        SubscribeNetworkEvent<VotePlayerListRequestEvent>(OnVotePlayerListRequestEvent);
    }

    private void OnVotePlayerListRequestEvent(VotePlayerListRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } entity
            || !_ghostQuery.HasComp(entity))
        {
            Log.Warning($"User {args.SenderSession.Name} sent a {nameof(VotePlayerListRequestEvent)} without being a ghost.");
            return;
        }

        List<(NetUserId, string)> players = new();

        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } attached)
                continue;

            if (attached == entity) continue;

            var playerInfo = $"({player.Name}) {Comp<MetaDataComponent>(attached).EntityName}";

            players.Add((player.UserId, playerInfo));
        }

        var response = new VotePlayerListResponseEvent(players.ToArray());
        RaiseNetworkEvent(response, args.SenderSession.Channel);
    }

    /// <summary>
    /// Used to check whether the player initiating a votekick is allowed to do so serverside.
    /// </summary>
    /// <param name="initiator">The session initiating the votekick.</param>
    public bool CheckVotekickInitEligibility(ICommonSession? initiator)
    {
        if (initiator == null)
            return false;

        if (!HasComp<GhostComponent>(initiator.AttachedEntity))
            return false;

        //if (await _dbManager.GetWhitelistStatusAsync(initiator.UserId)) TODO: Async? I 'ardly know 'er
        //    return false;

        return true;
    }

    /// <summary>
    /// Used to check whether the player being targetted for a votekick is a valid target.
    /// </summary>
    /// <param name="target">The session being targetted for a votekick.</param>
    public bool CheckVotekickTargetEligibility(ICommonSession? target)
    {
        if (target == null)
            return false;

        if (target.AttachedEntity != null && _adminManager.IsAdmin(target.AttachedEntity.Value))
            return false;

        return true;
    }
}
