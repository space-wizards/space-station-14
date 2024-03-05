using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.Roles.Jobs;
using Content.Server.Shuttles.Components;
using Content.Shared.Antag;
using Content.Shared.Humanoid;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.Chat;
using Robust.Shared.Enums;

namespace Content.Server.Antag;

public sealed class AntagSelectionSystem : GameRuleSystem<GameRuleComponent>
{
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;

    #region Eligible Player Selection
    /// <summary>
    /// Get all players that are eligible for an antag role
    /// </summary>
    /// <param name="playerSessions">All sessions from which to select eligible players</param>
    /// <param name="antagPrototype">The prototype to get eligible players for</param>
    /// <param name="includeAllJobs">Should jobs that prohibit antag roles (ie Heads, Sec, Interns) be included</param>
    /// <param name="acceptableAntags">Should players already selected as antags be eligible</param>
    /// <param name="ignorePreferences">Should we ignore if the player has enabled this specific role</param>
    /// <param name="customExcludeCondition">A custom condition that each player is tested against, if it returns true the player is excluded from eligibility</param>
    /// <returns>List of all player entities that match the requirements</returns>
    public List<EntityUid> GetEligiblePlayers(IEnumerable<ICommonSession> playerSessions,
        ProtoId<AntagPrototype> antagPrototype,
        bool includeAllJobs = false,
        AntagAcceptability acceptableAntags = AntagAcceptability.NotExclusive,
        bool ignorePreferences = false,
        bool allowNonHumanoids = false,
        Func<EntityUid?, bool>? customExcludeCondition = null)
    {
        var eligiblePlayers = new List<EntityUid>();

        foreach (var player in playerSessions)
        {
            if (IsPlayerEligible(player, antagPrototype, includeAllJobs, acceptableAntags, ignorePreferences, allowNonHumanoids, customExcludeCondition))
                eligiblePlayers.Add(player.AttachedEntity!.Value);
        }

        return eligiblePlayers;
    }

    /// <summary>
    /// Get all sessions that are eligible for an antag role, can be run prior to sessions being attached to an entity
    /// This does not exclude sessions that have already been chosen as antags - that must be handled manually
    /// </summary>
    /// <param name="playerSessions">All sessions from which to select eligible players</param>
    /// <param name="antagPrototype">The prototype to get eligible players for</param>
    /// <param name="ignorePreferences">Should we ignore if the player has enabled this specific role</param>
    /// <returns>List of all player sessions that match the requirements</returns>
    public List<ICommonSession> GetEligibleSessions(IEnumerable<ICommonSession> playerSessions, ProtoId<AntagPrototype> antagPrototype, bool ignorePreferences = false)
    {
        var eligibleSessions = new List<ICommonSession>();

        foreach (var session in playerSessions)
        {
            if (IsSessionEligible(session, antagPrototype, ignorePreferences))
                eligibleSessions.Add(session);
        }

        return eligibleSessions;
    }

    /// <summary>
    /// Test eligibility of the player for a specific antag role
    /// </summary>
    /// <param name="session">The player session to test</param>
    /// <param name="antagPrototype">The prototype to get eligible players for</param>
    /// <param name="includeAllJobs">Should jobs that prohibit antag roles (ie Heads, Sec, Interns) be included</param>
    /// <param name="acceptableAntags">Should players already selected as antags be eligible</param>
    /// <param name="ignorePreferences">Should we ignore if the player has enabled this specific role</param>
    /// <param name="customExcludeCondition">A function, accepting an EntityUid and returning bool. Each player is tested against this, returning truw will exclude the player from eligibility</param>
    /// <returns>True if the player session matches the requirements, false otherwise</returns>
    public bool IsPlayerEligible(ICommonSession session,
        ProtoId<AntagPrototype> antagPrototype,
        bool includeAllJobs = false,
        AntagAcceptability acceptableAntags = AntagAcceptability.NotExclusive,
        bool ignorePreferences = false,
        bool allowNonHumanoids = false,
        Func<EntityUid?, bool>? customExcludeCondition = null)
    {
        if (!IsSessionEligible(session, antagPrototype, ignorePreferences))
            return false;

        //Ensure the player has a mind
        if (session.GetMind() is not { } playerMind)
            return false;

        //Ensure the player has an attached entity
        if (session.AttachedEntity is not { } playerEntity)
            return false;

        //Ignore latejoined players, ie those on the arrivals station
        if (HasComp<PendingClockInComponent>(playerEntity))
            return false;

        //Exclude jobs that cannot be antag, unless explicitly allowed
        if (!includeAllJobs && !_jobs.CanBeAntag(session))
            return false;

        //Check if the entity is already an antag
        switch (acceptableAntags)
        {
            //If we dont want to select any antag roles
            case AntagAcceptability.None:
                {
                    if (_roleSystem.MindIsAntagonist(playerMind))
                        return false;
                    break;
                }
            //If we dont want to select exclusive antag roles
            case AntagAcceptability.NotExclusive:
                {
                    if (_roleSystem.MindIsExclusiveAntagonist(playerMind))
                        return false;
                    break;
                }
        }

        //Unless explictly allowed, ignore non humanoids (eg pets)
        if (!allowNonHumanoids && !HasComp<HumanoidAppearanceComponent>(playerEntity))
            return false;

        //If a custom condition was provided, test it and exclude the player if it returns true
        if (customExcludeCondition != null && customExcludeCondition(playerEntity))
            return false;


        return true;
    }

    /// <summary>
    /// Check if the session is eligible for a role, can be run prior to the session being attached to an entity
    /// </summary>
    /// <param name="session">Player session to check</param>
    /// <param name="antagPrototype">Which antag prototype to check for</param>
    /// <param name="ignorePreferences">Ignore if the player has enabled this antag</param>
    /// <returns>True if the session matches the requirements, false otherwise</returns>
    public bool IsSessionEligible(ICommonSession session, ProtoId<AntagPrototype> antagPrototype, bool ignorePreferences = false)
    {
        //Exclude disconnected or zombie sessions
        //No point giving antag roles to them
        if (session.Status == SessionStatus.Disconnected ||
            session.Status == SessionStatus.Zombie)
            return false;

        //Check the player has this antag preference selected
        //Unless we are ignoring preferences, in which case add them anyway
        var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(session.UserId).SelectedCharacter;
        if (!pref.AntagPreferences.Contains(antagPrototype.Id) && !ignorePreferences)
            return false;

        return true;
    }
    #endregion

    /// <summary>
    /// Helper method to calculate the number of antags to select based upon the number of players
    /// </summary>
    /// <param name="playerCount">How many players there are on the server</param>
    /// <param name="playersPerAntag">How many players should there be for an additional antag</param>
    /// <param name="maxAntags">Maximum number of antags allowed</param>
    /// <returns>The number of antags that should be chosen</returns>
    public int CalculateAntagCount(int playerCount, int playersPerAntag, int maxAntags)
    {
        return Math.Clamp(playerCount / playersPerAntag, 1, maxAntags);
    }

    #region Antag Selection
    /// <summary>
    /// Selects a set number of entities from several lists, prioritising the first list till its empty, then second list etc
    /// </summary>
    /// <param name="eligiblePlayerLists">Array of lists, which are chosen from in order until the correct number of items are selected</param>
    /// <param name="count">How many items to select</param>
    /// <returns>Up to the specified count of elements from all provided lists</returns>
    public List<EntityUid> ChooseAntags(int count, params List<EntityUid>[] eligiblePlayerLists)
    {
        var chosenPlayers = new List<EntityUid>();
        foreach (var playerList in eligiblePlayerLists)
        {
            //Remove all chosen players from this list, to prevent duplicates
            foreach (var chosenPlayer in chosenPlayers)
            {
                playerList.Remove(chosenPlayer);
            }

            //If we have reached the desired number of players, skip
            if (chosenPlayers.Count >= count)
                continue;

            //Pick and choose a random number of players from this list
            chosenPlayers.AddRange(ChooseAntags(count - chosenPlayers.Count, playerList));
        }
        return chosenPlayers;
    }
    /// <summary>
    /// Helper method to choose antags from a list
    /// </summary>
    /// <param name="eligiblePlayers">List of eligible players</param>
    /// <param name="count">How many to choose</param>
    /// <returns>Up to the specified count of elements from the provided list</returns>
    public List<EntityUid> ChooseAntags(int count, List<EntityUid> eligiblePlayers)
    {
        var chosenPlayers = new List<EntityUid>();

        for (var i = 0; i < count; i++)
        {
            if (eligiblePlayers.Count == 0)
                break;

            chosenPlayers.Add(RobustRandom.PickAndTake(eligiblePlayers));
        }

        return chosenPlayers;
    }

    /// <summary>
    /// Selects a set number of sessions from several lists, prioritising the first list till its empty, then second list etc
    /// </summary>
    /// <param name="eligiblePlayerLists">Array of lists, which are chosen from in order until the correct number of items are selected</param>
    /// <param name="count">How many items to select</param>
    /// <returns>Up to the specified count of elements from all provided lists</returns>
    public List<ICommonSession> ChooseAntags(int count, params List<ICommonSession>[] eligiblePlayerLists)
    {
        var chosenPlayers = new List<ICommonSession>();
        foreach (var playerList in eligiblePlayerLists)
        {
            //Remove all chosen players from this list, to prevent duplicates
            foreach (var chosenPlayer in chosenPlayers)
            {
                playerList.Remove(chosenPlayer);
            }

            //If we have reached the desired number of players, skip
            if (chosenPlayers.Count >= count)
                continue;

            //Pick and choose a random number of players from this list
            chosenPlayers.AddRange(ChooseAntags(count - chosenPlayers.Count, playerList));
        }
        return chosenPlayers;
    }
    /// <summary>
    /// Helper method to choose sessions from a list
    /// </summary>
    /// <param name="eligiblePlayers">List of eligible sessions</param>
    /// <param name="count">How many to choose</param>
    /// <returns>Up to the specified count of elements from the provided list</returns>
    public List<ICommonSession> ChooseAntags(int count, List<ICommonSession> eligiblePlayers)
    {
        var chosenPlayers = new List<ICommonSession>();

        for (int i = 0; i < count; i++)
        {
            if (eligiblePlayers.Count == 0)
                break;

            chosenPlayers.Add(RobustRandom.PickAndTake(eligiblePlayers));
        }

        return chosenPlayers;
    }
    #endregion

    #region Briefings
    /// <summary>
    /// Helper method to send the briefing text and sound to a list of entities
    /// </summary>
    /// <param name="entities">The players chosen to be antags</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    public void SendBriefing(List<EntityUid> entities, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        foreach (var entity in entities)
        {
            SendBriefing(entity, briefing, briefingColor, briefingSound);
        }
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a player entity
    /// </summary>
    /// <param name="entity">The entity chosen to be antag</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    public void SendBriefing(EntityUid entity, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        if (!_mindSystem.TryGetMind(entity, out _, out var mindComponent))
            return;

        if (mindComponent.Session == null)
            return;

        SendBriefing(mindComponent.Session, briefing, briefingColor, briefingSound);
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a list of sessions
    /// </summary>
    /// <param name="sessions"></param>
    /// <param name="briefing"></param>
    /// <param name="briefingColor"></param>
    /// <param name="briefingSound"></param>

    public void SendBriefing(List<ICommonSession> sessions, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        foreach (var session in sessions)
        {
            SendBriefing(session, briefing, briefingColor, briefingSound);
        }
    }
    /// <summary>
    /// Helper method to send the briefing text and sound to a session
    /// </summary>
    /// <param name="session">The player chosen to be an antag</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>

    public void SendBriefing(ICommonSession session, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        _audioSystem.PlayGlobal(briefingSound, session);
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", briefing));
        ChatManager.ChatMessageToOne(ChatChannel.Server, briefing, wrappedMessage, default, false, session.Channel, briefingColor);
    }
    #endregion
}
