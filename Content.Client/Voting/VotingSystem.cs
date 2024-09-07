using Content.Client.Ghost;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Voting;
using Robust.Client.Player;

namespace Content.Client.Voting;

public sealed class VotingSystem : EntitySystem
{

    public event Action<VotePlayerListResponseEvent>? VotePlayerListResponse; //Provides a list of players elligble for vote actions

    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly ISharedPlaytimeManager _playtimeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<VotePlayerListResponseEvent>(OnVotePlayerListResponseEvent);
    }

    private void OnVotePlayerListResponseEvent(VotePlayerListResponseEvent msg)
    {
        if (!_ghostSystem.IsGhost)
        {
            return;
        }

        VotePlayerListResponse?.Invoke(msg);
    }

    public void RequestVotePlayerList()
    {
        RaiseNetworkEvent(new VotePlayerListRequestEvent());
    }

    /// <summary>
    /// Used to check whether the player initiating a votekick is allowed to do so clientside.
    /// </summary>
    /// <returns></returns>
    public bool CheckVotekickInitEligibility()
    {
        // TODO: This should probably be a network event tbf
        if (!_ghostSystem.IsGhost)
        {
            return false;
        }

        if (_playerManager.LocalSession == null)
            return false;

        var playtime = _playtimeManager.GetPlayTimes(_playerManager.LocalSession);
        if (!playtime.TryGetValue(PlayTimeTrackingShared.TrackerOverall, out TimeSpan overallTime) || overallTime < TimeSpan.FromHours(10)) // TODO: Change timespan to cvar
            return false;

        // TODO: Check that the person initiating the vote is whitelisted

        return true;
    }
}
