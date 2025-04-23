using Robust.Shared.Configuration;

namespace Content.Shared.Starlight.CCVar;
public sealed partial class StarlightCCVars
{
    /// <summary>
    /// RoundEnd Vote
    /// </summary>
    public static readonly CVarDef<int> MinPlayerToVote = 
        CVarDef.Create("vote.min_player_to_vote", 2);
    
    public static readonly CVarDef<bool> ShowRestartVotes = 
        CVarDef.Create("vote.show_restart_votes", false);

    public static readonly CVarDef<bool> ShowPresetVotes = 
        CVarDef.Create("vote.show_preset_votes", false);

    public static readonly CVarDef<bool> ShowMapVotes = 
        CVarDef.Create("vote.show_map_votes", false);

    public static readonly CVarDef<bool> RunMapVoteAfterRestart = 
        CVarDef.Create("vote.run_map_vote_after_restart", false);

    public static readonly CVarDef<bool> RunPresetVoteAfterRestart = 
        CVarDef.Create("vote.run_preset_vote_after_restart", false);
    
    public static readonly CVarDef<int> VotingsDelay = 
        CVarDef.Create("vote.votings_delay", 90);
    
    public static readonly CVarDef<int> MapVotingCount = 
        CVarDef.Create("vote.map_voting_count", 3);
    
    public static readonly CVarDef<int> RoundVotingCount = 
        CVarDef.Create("vote.round_voting_count", 3);
        
    public static readonly CVarDef<string> MapVotingChancesPrototype = 
        CVarDef.Create("vote.map_voting_chances_prototype", "");
    
    public static readonly CVarDef<string> RoundVotingChancesPrototype = 
        CVarDef.Create("vote.round_voting_chances_prototype", "Basic");
        
    public static readonly CVarDef<bool> ResetPresetAfterRestart = 
        CVarDef.Create("game.reset_preset_after_restart", false);
}
