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
        TryCreateVoteOnRestart(CCVars.VoteAutoMapOnRoundEnd, "ui-vote-map-title", StandardVoteType.Map);
        TryCreateVoteOnRestart(CCVars.VoteAutoPresetOnRoundEnd, "ui-vote-gamemode-title", StandardVoteType.Preset);
    }

    private void TryCreateVoteOnRestart(CVarDef<bool> enabledCVar, string titleKey, StandardVoteType type)
    {
        if (!_cfg.GetCVar(enabledCVar))
            return;

        var title = Loc.GetString(titleKey);

        if (_voteManager.ActiveVotes.All(v => !v.Type.Equals(type)))
        {
            _voteManager.CreateStandardVote(null, type);
        }
    }
}
