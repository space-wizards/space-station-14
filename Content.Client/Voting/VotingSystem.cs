using Content.Client.Ghost;
using Content.Shared.Voting;

namespace Content.Client.Voting;

public sealed class VotingSystem : EntitySystem
{

    public event Action<VotePlayerListResponseEvent>? VotePlayerListResponse; //Provides a list of players elligble for vote actions

    [Dependency] private readonly GhostSystem _ghostSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<VotePlayerListResponseEvent>(OnVotePlayerListResponseEvent);
    }

    private void OnVotePlayerListResponseEvent(VotePlayerListResponseEvent msg)
    {
        VotePlayerListResponse?.Invoke(msg);
    }

    public void RequestVotePlayerList()
    {
        RaiseNetworkEvent(new VotePlayerListRequestEvent());
    }
}
