using System.Linq;
using Content.Server.Antag.Components;
using Content.Shared.Antag;
using Content.Shared.Chat;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    /// <inhereitdoc cref="GetActivePlayerCount(IList{ICommonSession})"/>
    [PublicAPI]
    public int GetActivePlayerCount()
    {
        return GetActivePlayerCount(_playerManager.Sessions);
    }

    /// <summary>
    /// Returns the total number of valid players from the given player pool.
    /// For a player to be valid, they must have a connection to the server, be in the round, and have a non-ghost entity.
    /// </summary>
    /// <param name="pool">Player pool we're querying, this typically includes all players connected to the server.</param>
    /// <returns>The number of valid players</returns>
    [PublicAPI]
    public int GetActivePlayerCount(IList<ICommonSession> pool)
    {
        var count = 0;
        foreach (var session in pool)
        {
            if (IsDisconnected(session))
                continue;

            if (session.AttachedEntity is not { } uid || HasComp<GhostComponent>(uid))
                continue;

            count++;
        }

        return count;
    }

    [PublicAPI]
    public IEnumerable<ICommonSession> GetActivePlayers()
    {
        return GetActivePlayers(_playerManager.Sessions);
    }

    [PublicAPI]
    public IEnumerable<ICommonSession> GetActivePlayers(IList<ICommonSession> pool)
    {
        foreach (var session in pool)
        {
            if (IsDisconnected(session))
                continue;

            if (session.AttachedEntity is not { } uid || HasComp<GhostComponent>(uid))
                continue;

            yield return session;
        }
    }

    public bool IsDisconnected(ICommonSession session)
    {
        return session.Status is SessionStatus.Disconnected or SessionStatus.Zombie;
    }

    /// <summary>
    /// Gets the total number of antags a game rule wishes to spawn.
    /// </summary>
    /// <param name="gameRule">Game rule which is spawning antags</param>
    /// <param name="playerCount">Simulated player count</param>
    /// <returns>Total number of antags this gamerule will spawn</returns>
    [PublicAPI]
    public int GetTotalAntagCount(Entity<AntagSelectionComponent> gameRule, int playerCount)
    {
        var runningCount = 0;
        var count = 0;

        // We assume that antag definitions are prioritized by order, and take up slots that other roles may take.
        // I.E for Nukies, it selects 1 commander which takes up 10 players, then one corpsman which takes up another 10, then we select X nukies based on the remaining player count.
        // This is how the system worked when I got here, and I decided not to change it to avoid fucking with team antag balance
        foreach (var definition in gameRule.Comp.Antags)
        {
            if (!Proto.Resolve(definition, out var antag))
                continue;

            count += GetTargetAntagCount(antag, playerCount, ref runningCount);
            runningCount += count * antag.PlayerRatio;
        }

        return count;
    }

    /// <summary>
    /// Gets the number of antags of a given type this game rule is attempting to spawn, for a given player count.
    /// </summary>
    /// <param name="gameRule">Game rule which is spawning the antags</param>
    /// <param name="playerCount">Current player count</param>
    /// <param name="proto">Antag prototype we're spawning</param>
    /// <returns>Number of antags of this type we're spawning.</returns>
    [PublicAPI]
    public int GetTargetAntagCount(Entity<AntagSelectionComponent> gameRule, int playerCount, ProtoId<AntagSpecifierPrototype> proto)
    {
        if (!Proto.Resolve(proto, out var antag))
            return 0;

        return GetTargetAntagCount(gameRule, playerCount, antag);
    }

    /// <inheritdoc cref="GetTargetAntagCount(Entity{AntagSelectionComponent},int,ProtoId{AntagSpecifierPrototype})"/>
    [PublicAPI]
    public int GetTargetAntagCount(Entity<AntagSelectionComponent> gameRule, int playerCount, AntagSpecifierPrototype proto)
    {
        var runningCount = 0;

        // We assume that antag definitions are prioritized by order, and take up slots that other roles may take.
        // I.E for Nukies, it selects 1 commander which takes up 10 players, then one corpsman which takes up another 10, then we select X nukies based on the remaining player count.
        // This is how the system worked when I got here, and I decided not to change it to avoid fucking with team antag balance
        foreach (var definition in gameRule.Comp.Antags)
        {
            if (!Proto.Resolve(definition, out var antag))
                continue;

            // We need to update our running count which is why we get the count for definitions we may not be assigning.
            var count = GetTargetAntagCount(antag, playerCount, ref runningCount);

            if (definition == proto)
                return count;
        }

        Log.Error($"Error, attempted to get the antag count for an antagonist, {proto.ID} not included in gamerule: {ToPrettyString(gameRule)}");
        return 0;
    }

    /// <summary>
    /// Do not use this if you don't know what you're doing. This is public for test purposes only.
    /// </summary>
    public int GetTargetAntagCount(AntagSpecifierPrototype definition, int playerCount, ref int runningCount)
    {
        var count = GetTargetAntagCount(definition, playerCount - runningCount);
        runningCount += count * definition.PlayerRatio;
        return count;
    }

    private int GetTargetAntagCount(AntagSpecifierPrototype definition, int playerCount)
    {
        return Math.Clamp(playerCount / definition.PlayerRatio, definition.Range.Min, definition.Range.Max);
    }

    /// <summary>
    /// Gets the total number of assigned antags of a given type from a game rule.
    /// </summary>
    /// <param name="gameRule">Game rule entity</param>
    /// <param name="proto">The antag prototype we're checking</param>
    /// <returns>The amount of sessions which this game rule has assigned our given prototype to.</returns>
    [PublicAPI]
    public int GetAssignedAntagCount(Entity<AntagSelectionComponent> gameRule, ProtoId<AntagSpecifierPrototype> proto)
    {
        return !gameRule.Comp.AssignedMinds.TryGetValue(proto, out var assigned) ? 0 : assigned.Count;
    }

    /// <summary>
    /// Checks if all antags of this specific type from this specific game rule have been assigned.
    /// </summary>
    [PublicAPI]
    public bool AllAntagsAssigned(Entity<AntagSelectionComponent> gameRule, AntagSpecifierPrototype proto, int players)
    {
        return GetAssignedAntagCount(gameRule, proto) < GetTargetAntagCount(gameRule, players, proto);
    }

    /// <summary>
    /// Returns identifiable information for all antagonists to be used in a round end summary.
    /// </summary>
    /// <returns>
    /// A list containing, in order, the antag's mind, the session data, and the original name stored as a string.
    /// </returns>
    [PublicAPI]
    public IEnumerable<(EntityUid, SessionData, string)> GetAntagIdentifiers(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            yield break;

        foreach (var (_, minds) in ent.Comp.AssignedMinds)
        {
            foreach (var (mind, name) in minds)
            {
                if (!TryComp<MindComponent>(mind, out var mindComp) || mindComp.OriginalOwnerUserId == null)
                    continue;

                if (!_playerManager.TryGetPlayerData(mindComp.OriginalOwnerUserId.Value, out var data))
                    continue;

                yield return (mind, data, name);
            }
        }
    }

    /// <summary>
    /// Returns identifiable information for all antagonists to be used in a round end summary.
    /// </summary>
    /// <returns>
    /// A list containing, in order, the antag's mind, the session data, and the original name stored as a string.
    /// </returns>
    [PublicAPI]
    public IEnumerable<(EntityUid, string)> GetAntagIdentities(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            yield break;

        foreach (var (_, minds) in ent.Comp.AssignedMinds)
        {
            foreach (var identity in minds)
            {
                yield return identity;
            }
        }
    }

    /// <summary>
    /// Returns all the minds of antagonists.
    /// </summary>
    [PublicAPI]
    public IEnumerable<Entity<MindComponent>> GetAntagMinds(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            yield break;

        foreach (var (_, minds) in ent.Comp.AssignedMinds)
        {
            foreach (var (mind, _) in minds)
            {
                if (!TryComp<MindComponent>(mind, out var mindComp) || mindComp.OriginalOwnerUserId == null)
                    continue;

                yield return (mind, mindComp);
            }
        }
    }

    /// <summary>
    /// Returns all the antagonists for this rule who are currently alive
    /// </summary>
    [PublicAPI]
    public IEnumerable<EntityUid> GetAliveAntags(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            yield break;

        var minds = GetAntagMinds(ent);
        foreach (var mind in minds)
        {
            if (_mind.IsCharacterDeadIc(mind))
                continue;

            if (mind.Comp.OriginalOwnedEntity != null)
                yield return GetEntity(mind.Comp.OriginalOwnedEntity.Value);
        }
    }

    /// <summary>
    /// Returns the number of alive antagonists for this rule.
    /// </summary>
    [PublicAPI]
    public int GetAliveAntagCount(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0;

        var numbah = 0;
        var minds = GetAntagMinds(ent);
        foreach (var mind in minds)
        {
            if (_mind.IsCharacterDeadIc(mind))
                continue;

            numbah++;
        }

        return numbah;
    }

    /// <summary>
    /// Returns if there are any remaining antagonists alive for this rule.
    /// </summary>
    [PublicAPI]
    public bool AnyAliveAntags(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return GetAliveAntags(ent).Any();
    }

    /// <summary>
    /// Checks if all the antagonists for this rule are alive.
    /// </summary>
    [PublicAPI]
    public bool AllAntagsAlive(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return GetAliveAntagCount(ent) == ent.Comp.AssignedMinds.Count;
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a player entity
    /// </summary>
    /// <param name="entity">The entity chosen to be antag</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    [PublicAPI]
    public void SendBriefing(EntityUid entity, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        if (!_mind.TryGetMind(entity, out _, out var mindComponent))
            return;

        if (!_playerManager.TryGetSessionById(mindComponent.UserId, out var session))
            return;

        SendBriefing(session, briefing, briefingColor, briefingSound);
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a list of sessions
    /// </summary>
    /// <param name="sessions">The sessions that will be sent the briefing</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    [PublicAPI]
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
    /// <param name="data">The briefing data</param>
    private void SendBriefing(
        ICommonSession? session,
        BriefingData? data)
    {
        if (session == null || data == null)
            return;

        var text = data.Value.Text == null ? string.Empty : Loc.GetString(data.Value.Text);
        SendBriefing(session, text, data.Value.Color, data.Value.Sound);
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a session
    /// </summary>
    /// <param name="session">The player chosen to be an antag</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    // TODO: It might take a bit of effort but this can probably be privated.
    public void SendBriefing(
        ICommonSession? session,
        string briefing,
        Color? briefingColor,
        SoundSpecifier? briefingSound)
    {
        if (session == null)
            return;

        _audio.PlayGlobal(briefingSound, session);
        if (!string.IsNullOrEmpty(briefing))
        {
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", briefing));
            _chat.ChatMessageToOne(ChatChannel.Server, briefing, wrappedMessage, default, false, session.Channel, briefingColor);
        }
    }

    /// <summary>
    /// Returns a list of all antag players who have blacklisted jobs, and a hashset of those blacklisted jobs.
    /// </summary>
    /// <param name="except">Antag prototypes we're excluding for our returned job blacklist.</param>
    /// <returns>A dictionary of antag sessions, and their job blacklists.</returns>
    [PublicAPI]
    public Dictionary<ICommonSession, HashSet<ProtoId<JobPrototype>>> GetAntagBlockedJobs(params HashSet<ProtoId<AntagSpecifierPrototype>> except)
    {
        var result = new Dictionary<ICommonSession, HashSet<ProtoId<JobPrototype>>>();
        var query = QueryAllRules();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            if (HasComp<EndedGameRuleComponent>(uid))
                continue;

            foreach (var def in comp.Antags)
            {
                if (except.Contains(def))
                    continue;

                if (!comp.PreSelectedSessions.TryGetValue(def, out var set) || !Proto.Resolve(def, out var antag))
                    continue;

                if (antag.JobBlacklist.Count == 0)
                    continue;

                foreach (var player in set)
                {
                    if (result.TryGetValue(player, out var jobs))
                        jobs.UnionWith(antag.JobBlacklist);
                    else
                        result.Add(player, antag.JobBlacklist);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Returns a list of all blocked jobs for this player due to antags.
    /// </summary>
    /// <param name="player">Player we're checking the blocked jobs of</param>
    /// <param name="except">Antag prototypes we're excluding in our search</param>
    /// <returns>A hashset of all blocked jobs for this player.</returns>
    [PublicAPI]
    public HashSet<ProtoId<JobPrototype>> GetAntagBlockedJobs(ICommonSession player, params HashSet<ProtoId<AntagSpecifierPrototype>> except)
    {
        var result = new HashSet<ProtoId<JobPrototype>>();
        var query = QueryAllRules();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            if (HasComp<EndedGameRuleComponent>(uid))
                continue;

            foreach (var def in comp.Antags)
            {
                if (except.Contains(def))
                    continue;

                if (!comp.PreSelectedSessions.TryGetValue(def, out var set) || !Proto.Resolve(def, out var antag))
                    continue;

                if (antag.JobBlacklist.Count == 0)
                    continue;

                if (set.Contains(player))
                    result.UnionWith(antag.JobBlacklist);
            }
        }

        return result;
    }

    /// <summary>
    /// Get all sessions that have been preselected for antag.
    /// </summary>
    /// <param name="except">A specific definition to be excluded from the check.</param>
    [PublicAPI]
    public HashSet<ICommonSession> GetPreSelectedAntagSessions(params HashSet<ProtoId<AntagSpecifierPrototype>> except)
    {
        var result = new HashSet<ICommonSession>();
        var query = QueryAllRules();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            if (HasComp<EndedGameRuleComponent>(uid))
                continue;

            foreach (var def in comp.Antags)
            {
                if (except.Contains(def))
                    continue;

                if (comp.PreSelectedSessions.TryGetValue(def, out var set))
                    result.UnionWith(set);
            }
        }

        return result;
    }

    public bool TryGetValidAntagPreferences(ICommonSession session, List<ProtoId<AntagPrototype>>? filter = null)
    {
        return TryGetValidAntagPreferences(session, out _, filter);
    }

    /// <summary>
    /// Gets the antag preferences for a specific session, excluding banned antags or antags this player lacks the playtime for.
    /// </summary>
    /// <param name="session">Session we want the antag preferences for</param>
    /// <param name="antags">List of valid antag prototypes this player can play as.</param>
    /// <param name="filter">Optional list of antag prototypes we're specifically looking for.</param>
    /// <returns>True if this player has any antags enabled they can play and pass our filter</returns>
    [PublicAPI]
    public bool TryGetValidAntagPreferences(ICommonSession session, out List<ProtoId<AntagPrototype>> antags, List<ProtoId<AntagPrototype>>? filter = null)
    {
        antags = new List<ProtoId<AntagPrototype>>(GetValidAntagPreferences(session, filter));
        return antags.Count > 0;
    }

    /// <summary>
    /// Gets the antag preferences for a specific session, excluding banned antags or antags this player lacks the playtime for.
    /// Optionally takes a filter for antag preferences we're specifically looking for.
    /// </summary>
    /// <param name="session">Session we want the antag preferences for</param>
    /// <param name="filter">Optional list of antag preferences we're specifically looking for.</param>
    /// <returns>A list of all antags which the player meets the requirement for, and are contained in the filter</returns>
    [PublicAPI]
    public IEnumerable<ProtoId<AntagPrototype>> GetValidAntagPreferences(ICommonSession session, List<ProtoId<AntagPrototype>>? filter = null)
    {
        if (!_pref.TryGetCachedPreferences(session.UserId, out var prefs))
            yield break;

        foreach (var antag in prefs.SelectedCharacter.AntagPreferences)
        {
            // We also check this in IsSessionValid, but we also check it here since this is public API.
            if (_ban.IsRoleBanned(session, antag) || !_playTime.IsAllowed(session, antag))
                continue;

            if (filter != null && !filter.Contains(antag))
                continue;

            yield return antag;
        }
    }

    /// <summary>
    /// Checks if a player has been assigned antag for a specific game rule.
    /// Does not check if that game rule is active or ended so check that beforehand if it matters.
    /// </summary>
    /// <returns></returns>
    [PublicAPI]
    public bool IsAssignedAntag(Entity<AntagSelectionComponent> gameRule, ICommonSession player)
    {
        // First check our mindroles.
        if (_role.MindIsAntagonist(player.AttachedEntity))
            return true;

        foreach (var (_, sessions) in gameRule.Comp.PreSelectedSessions)
        {
            // Session has already been preselected as antagonist, and therefore *has* been assigned antag!
            if (sessions.Contains(player))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a player has been assigned a specific antag for a specific game rule.
    /// Does not check if that game rule is active or ended so check that beforehand if it matters.
    /// Also does not check mind roles, but if the game rule data is messed up you have bigger problems.
    /// </summary>
    /// <returns></returns>
    [PublicAPI]
    public bool IsAssignedAntag(Entity<AntagSelectionComponent> gameRule, ProtoId<AntagSpecifierPrototype> antag, ICommonSession player)
    {
        if (!gameRule.Comp.PreSelectedSessions.TryGetValue(antag, out var sessions))
            return false;

        // Session has already been preselected as antagonist, and therefore *has* been assigned antag!
        return sessions.Contains(player);
    }

    /// <summary>
    /// Checks if the given player is currently assigned antag for any game rule.
    /// </summary>
    /// <param name="player">Player who may or may not be the antagonist.</param>
    /// <returns>True if there is a game rule giving this player antag status</returns>
    [PublicAPI]
    public bool IsAssignedAntag(ICommonSession player)
    {
        // First check our mindroles.
        if (_role.MindIsAntagonist(player.AttachedEntity))
            return true;

        var query = QueryAllRules();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            if (HasComp<EndedGameRuleComponent>(uid))
                continue;

            foreach (var (_, sessions) in comp.PreSelectedSessions)
            {
                // Session has already been preselected as antagonist, and therefore *has* been assigned antag!
                if (sessions.Contains(player))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the given player is currently assigned antag for any game rule that is incompatible with other antag prototypes.
    /// </summary>
    /// <param name="player">Player who may or may not be the antagonist.</param>
    /// <returns>True if there is a game rule giving this player antag status that is exclusive with other antags</returns>
    [PublicAPI]
    public bool IsAssignedExclusiveAntag(ICommonSession player)
    {
        // First check our mindroles.
        if (_role.MindIsExclusiveAntagonist(player.AttachedEntity))
            return true;

        var query = QueryAllRules();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            if (HasComp<EndedGameRuleComponent>(uid))
                continue;

            foreach (var (proto, sessions) in comp.PreSelectedSessions)
            {
                if (!Proto.Resolve(proto, out var def))
                    continue; // How did you even get here?

                if (!sessions.Contains(player))
                    continue;

                if (def.MultiAntagSetting == AntagAcceptability.None)
                    return true;
            }
        }

        return false;
    }
}
