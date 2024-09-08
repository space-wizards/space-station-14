using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.Ghost;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Voting;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Threading.Tasks;

namespace Content.Server.Voting;

public sealed class VotingSystem : EntitySystem
{

    private EntityQuery<GhostComponent> _ghostQuery;

    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();

        SubscribeNetworkEvent<VotePlayerListRequestEvent>(OnVotePlayerListRequestEvent);
    }

    private async void OnVotePlayerListRequestEvent(VotePlayerListRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } entity
            || !await CheckVotekickInitEligibility(args.SenderSession))
        {
            var deniedResponse = new VotePlayerListResponseEvent(new (NetUserId, NetEntity, string)[0], true);
            RaiseNetworkEvent(deniedResponse, args.SenderSession.Channel);
            return;
        }

        List<(NetUserId, NetEntity, string)> players = new();

        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } attached)
                continue;

            if (attached == entity) continue;

            var playerInfo = $"({player.Name}) {Comp<MetaDataComponent>(attached).EntityName}";
            var netEntity = GetNetEntity(attached);

            players.Add((player.UserId, netEntity, playerInfo));
        }

        var response = new VotePlayerListResponseEvent(players.ToArray(), false);
        RaiseNetworkEvent(response, args.SenderSession.Channel);
    }

    /// <summary>
    /// Used to check whether the player initiating a votekick is allowed to do so serverside.
    /// </summary>
    /// <param name="initiator">The session initiating the votekick.</param>
    public async Task<bool> CheckVotekickInitEligibility(ICommonSession? initiator)
    {
        if (initiator == null)
            return false;

        if (!TryComp(initiator.AttachedEntity, out GhostComponent? ghostComp))
            return false;

        if ((int)_gameTiming.RealTime.Subtract(ghostComp.TimeOfDeath).TotalSeconds < _cfg.GetCVar(CCVars.VotekickEligibleVoterDeathtime))
            return false;

        if (!await _dbManager.GetWhitelistStatusAsync(initiator.UserId))
            return false;

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
