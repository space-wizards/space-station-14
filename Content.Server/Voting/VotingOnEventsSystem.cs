using System.Linq;
using Content.Server.Voting.Managers;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Voting;
using Robust.Shared.Configuration;

namespace Content.Server.Voting;

/// <summary>
/// Handles creating votes on certain events.
/// </summary>
public sealed class VotingOnEventsSystem : EntitySystem
{
    [Dependency] private readonly IVoteManager _voteManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartingEvent>(OnRoundRestartEvent);
    }

    public void OnRoundRestartEvent(ref RoundRestartingEvent ev)
    {
        TryCreateVoteOnRestart(CCVars.VoteAutoMapOnRoundEnd, StandardVoteType.Map);
        TryCreateVoteOnRestart(CCVars.VoteAutoPresetOnRoundEnd, StandardVoteType.Preset);
    }

    private void TryCreateVoteOnRestart(CVarDef<bool> enabledCVar, StandardVoteType type)
    {
        if (!_cfg.GetCVar(enabledCVar))
            return;

        if (_voteManager.ActiveVotes.All(v => !v.Type.Equals(type)))
        {
            _voteManager.CreateStandardVote(null, type);
        }
    }
}
