using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.Voting.Managers;
using Content.Shared.Starlight.CCVar;
using Content.Shared.Voting;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;

namespace Content.Server.Starlight.GameTicking;

public sealed class RoundEndVoteSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private TimeSpan? _voteStartTime;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndSystemChangedEvent>(OnRoundEndSystemChange);
    }

    public void OnRoundEndSystemChange(RoundEndSystemChangedEvent args)
    {
        _voteStartTime = _gameTiming.CurTime + _gameTicker.LobbyDuration - TimeSpan.FromSeconds(_cfg.GetCVar(StarlightCCVars.VotingsDelay));
        Log.Warning($"Vote will start at {_voteStartTime}");

        if (_cfg.GetCVar(StarlightCCVars.ResetPresetAfterRestart))
            _gameTicker.SetGamePreset("Secret");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby || _voteStartTime == null)
            return;

        if (_gameTiming.CurTime >= _voteStartTime)
        {
            StartRoundEndVotes();
            _voteStartTime = null;
        }
    }

    public void StartRoundEndVotes()
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
            return;

        if (_cfg.GetCVar(StarlightCCVars.RunMapVoteAfterRestart))
            _voteManager.CreateStandardVote(null, StandardVoteType.Map);

        if (_cfg.GetCVar(StarlightCCVars.RunPresetVoteAfterRestart))
            _voteManager.CreateStandardVote(null, StandardVoteType.Preset);
    }
}