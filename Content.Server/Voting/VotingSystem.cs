using Content.Server.Ghost;
using Content.Shared.Ghost;
using Content.Shared.Voting;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server.Voting;

public sealed class VotingSystem : EntitySystem
{

    private EntityQuery<GhostComponent> _ghostQuery;

    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

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

            var playerInfo = $"{player.Name} {Comp<MetaDataComponent>(attached).EntityName}";

            players.Add((player.UserId, playerInfo));
        }

        var response = new VotePlayerListResponseEvent(players.ToArray());
        RaiseNetworkEvent(response, args.SenderSession.Channel);
    }
}
