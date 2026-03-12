using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Antag.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Antag;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Players;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using static Content.Server.Antag.Components.AntagSelectionTime;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    /// <inhereitdoc cref="CanBeAntag(ICommonSession,Entity{AntagSelectionComponent},AntagSpecifierPrototype,bool)"/>
    [PublicAPI]
    public bool CanBeAntag(ICommonSession player, AntagRule antagRule, bool checkPref = true)
    {
        return CanBeAntag(player, antagRule.GameRule, antagRule.Definition, checkPref);
    }

    /// <inhereitdoc cref="CanBeAntag(ICommonSession,Entity{AntagSelectionComponent},AntagSpecifierPrototype,bool)"/>
    [PublicAPI]
    public bool CanBeAntag(ICommonSession player,
        Entity<AntagSelectionComponent> gameRule,
        ProtoId<AntagSpecifierPrototype> proto,
        bool checkPref = true)
    {
        // Can't be this antag if it doesn't exist :)
        if (!Proto.Resolve(proto, out var antag))
            return false;

        return CanBeAntag(player, gameRule, antag, checkPref);
    }

    /// <summary>
    /// Verifies that a player is able to be an antag performing a wide variety of checks.
    /// </summary>
    /// <param name="player">Player we're checking</param>
    /// <param name="gameRule">Game rule which we want to be an antag for, needed to ensure we haven't already been selected.</param>
    /// <param name="def">Antag definition we want to become</param>
    /// <param name="checkPref">Whether we want to check our antag preferences or not.</param>
    /// <returns>True if this player can be an antagonist.</returns>
    [PublicAPI]
    public bool CanBeAntag(ICommonSession player,
        Entity<AntagSelectionComponent> gameRule,
        AntagSpecifierPrototype def,
        bool checkPref = true)
    {
        if (!IsSessionValid(player, def))
            return false;

        if (IsAssignedAntag(gameRule, def, player))
            return false;

        // Add player to the appropriate antag pool
        if (checkPref && !TryGetValidAntagPreferences(player, def.PrefRoles))
            return false;

        return true;
    }

    /// <inhereitdoc cref="IsSessionValid(ICommonSession,ProtoId{AntagSpecifierPrototype})"/>
    public bool IsSessionValid(ICommonSession session,
        ProtoId<AntagSpecifierPrototype> def)
    {
        if (!Proto.Resolve(def, out var antag))
            return false;

        return IsSessionValid(session, antag);
    }

    /// <summary>
    /// Checks if our session can play a given antagonist, checking if the session is role banned from the antag,
    /// </summary>
    /// <param name="session">Session which we are checking antag viability for</param>
    /// <param name="def">Antag definition we're checking against.</param>
    /// <returns>True if there is nothing stopping this session from becoming this antagonist.</returns>
    [PublicAPI]
    public bool IsSessionValid(ICommonSession session,
        AntagSpecifierPrototype def)
    {
        // Cannot be antag if you're not in the game.
        if (IsDisconnected(session))
            return false;

        if (IsAntagBanned(session, def))
            return false;

        // If our antag is mutually exclusive with other antags, yell about it!
        switch (def.MultiAntagSetting)
        {
            case AntagAcceptability.None:
            {
                if (IsAssignedAntag(session))
                    return false;
                break;
            }
            case AntagAcceptability.NotExclusive:
            {
                if (IsAssignedExclusiveAntag(session))
                    return false;
                break;
            }
        }

        return session.AttachedEntity == null || IsEntityValid(session, def);
    }

    /// <inhereitdoc cref="IsMindValid(EntityUid?,AntagSpecifierPrototype)"/>
    public bool IsMindValid(ICommonSession session, AntagSpecifierPrototype def)
    {
        return IsMindValid(session.GetMind(), def);
    }

    /// <summary>
    /// Checks if the given mind entity is valid for the specified antag.
    /// </summary>
    /// <param name="mind">Mind we are checking</param>
    /// <param name="def">Antag definition we want to give this mind.</param>
    /// <returns>True if there is nothing stopping this mind entity from being this antag.</returns>
    private bool IsMindValid([NotNullWhen(true)] EntityUid? mind, AntagSpecifierPrototype def)
    {
        // "Sorry buddy, but you can't be a traitor and the head of security" - Urist 1984
        // This checks nullability for our mind for free as well!
        if (_jobs.MindTryGetJob(mind, out var job) && def.JobBlacklist.Contains(job))
            return false;

        return true;
    }

    /// <summary>
    /// Checks both the mind and attached entity of the given session to see if anything is blocking it from being converted to an antag.
    /// </summary>
    /// <param name="session">Entity whose validity we're checking.</param>
    /// <param name="def">Antag definition we want to give them.</param>
    /// <returns>True if there is nothing stopping this entity from being this antag. Or if there is no entity.</returns>
    public bool IsEntityValid(ICommonSession session, AntagSpecifierPrototype def)
    {
        return IsMindValid(session, def) && IsEntityValid(session.AttachedEntity, def);
    }

    /// <inhereitdoc cref="IsEntityValid(EntityUid?,AntagSpecifierPrototype)"/>
    public bool IsEntityValid([NotNullWhen(true)] EntityUid? uid, ProtoId<AntagSpecifierPrototype> def)
    {
        if (!Proto.Resolve(def, out var antag))
            return false;

        return IsEntityValid(uid, antag);
    }

    /// <summary>
    /// Checks if the given entity is able to become the given antagonist.
    /// Note that this does not check if the entity had a mind or if that mind can become an antag.
    /// </summary>
    /// <param name="uid">Entity whose validity we're checking.</param>
    /// <param name="def">Antag definition we want to give them.</param>
    /// <returns>True if there is nothing stopping this entity from being this antag. Or if there is no entity.</returns>
    public bool IsEntityValid([NotNullWhen(true)] EntityUid? uid, AntagSpecifierPrototype def)
    {
        // If the player has not spawned in as any entity (e.g., in the lobby), they can be given an antag role/entity.
        if (!_whitelist.CheckBoth(uid, def.Blacklist, def.Whitelist))
            return false;

        if (_arrivals.IsOnArrivals((uid.Value, null)))
            return false;

        if (!def.AllowNonHumans && !HasComp<HumanoidProfileComponent>(uid))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if our session is banned from playing this antag. If so returns true.
    /// This is separate so that methods which normally force antag can return early.
    /// </summary>
    /// <param name="session">Player who may or may not be banned from an antagonist</param>
    /// <param name="definition">The definition which a player may or may not be banned from</param>
    /// <returns>True if any of the preferred roles within the definition hit a ban.</returns>
    [PublicAPI]
    public bool IsAntagBanned(ICommonSession session, AntagSpecifierPrototype definition)
    {
        if (_ban.GetAntagBans(session.UserId) is not { } bans)
            return false;

        foreach (var role in definition.PrefRoles)
        {
            // banned!
            if (bans.Contains(role))
                return true;
        }

        return false;
    }

    /// <inheritdoc cref="TryMakeAntag(Entity{AntagSelectionComponent},AntagSpecifierPrototype,ICommonSession,bool)"/>
    [PublicAPI]
    public bool TryMakeAntag(Entity<AntagSelectionComponent> gameRule,
        ProtoId<AntagSpecifierPrototype> proto,
        ICommonSession session,
        bool checkPref = true)
    {
        if (!Proto.Resolve(proto, out var def))
            return false;

        return TryMakeAntag(gameRule, def, session, checkPref);
    }

    /// <summary>
    /// Tries to make a given player into the specified antagonist for the given game rule.
    /// </summary>
    [PublicAPI]
    public bool TryMakeAntag(Entity<AntagSelectionComponent> gameRule,
        AntagSpecifierPrototype prototype,
        ICommonSession session,
        bool checkPref = true)
    {
        _adminLogger.Add(LogType.AntagSelection,
            $"Start trying to make {session} become the antagonist: {ToPrettyString(gameRule)}, {prototype.ID}");

        if (!CanBeAntag(session, gameRule, prototype, checkPref))
            return false;

        PreSelectSession(gameRule, prototype, session);
        return TryInitializeAntag(gameRule, prototype, session);
    }

    /// <inheritdoc cref="TryAssignNextAvailableAntag(Entity{AntagSelectionComponent},ICommonSession,int)"/>
    public bool TryAssignNextAvailableAntag(Entity<AntagSelectionComponent> gameRule, ICommonSession session)
    {
        return TryAssignNextAvailableAntag(gameRule, session, GetActivePlayerCount());
    }

    /// <summary>
    /// Tries to find an open antag slot for a given player and assign it to that player.
    /// </summary>
    /// <param name="gameRule">GameRule we are checking for antags.</param>
    /// <param name="session">Player we're trying to assign antag to.</param>
    /// <param name="players">Current number of players in the round. Used to determine antag count.</param>
    /// <returns>Returns true if an open antag slot was found and successfully assigned, false otherwise.</returns>
    public bool TryAssignNextAvailableAntag(Entity<AntagSelectionComponent> gameRule,
        ICommonSession session,
        int players)
    {
        foreach (var selector in gameRule.Comp.Antags)
        {
            if (!Proto.Resolve(selector.Proto, out var antag))
                continue;

            // Because this value can theoretically fluctuate as players leave and join, we don't want to cache it.
            if (AllAntagsAssigned(gameRule, antag, players))
                continue;

            // Try and assign this antag, if we fail, then try the next definition!
            if (TryMakeAntag(gameRule, antag, session))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Attempt to make this player be a late-join antag.
    /// </summary>
    /// <param name="session">The session to attempt to make antag.</param>
    [PublicAPI]
    public bool TryMakeLateJoinAntag(ICommonSession session)
    {
        // Sorry buddy, no antag for you!
        if (!RobustRandom.Prob(LateJoinRandomChance))
            return false;

        // TODO: We may want to query all rules to add late joins to pre-selections?
        // This logic is effectively copy-pasted from the old system with some fixes.
        var query = QueryActiveRules();
        var rules = new List<(EntityUid, AntagSelectionComponent)>();
        while (query.MoveNext(out var uid, out _, out var antag, out _))
        {
            // This is intended to only be used for ghost roles so it shouldn't be assigned for late joins
            if (antag.SelectionTime == Never || !antag.LateJoinAdditional)
                continue;

            rules.Add((uid, antag));
        }

        RobustRandom.Shuffle(rules);

        var players = GetActivePlayerCount();

        foreach (var (uid, antag) in rules)
        {
            if (TryAssignNextAvailableAntag((uid, antag), session, players))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Takes a list of AntagRules and tries to make ghost roles out of them.
    /// </summary>
    /// <param name="antagRules">list of antag rules we wish to turn into ghost roles.
    /// Note, a ghost role can only be created if the rule has the ghost role spawner protoId set to a valid prototype.</param>
    [PublicAPI]
    public void SpawnGhostRoles(List<AntagRule> antagRules)
    {
        foreach (var rule in antagRules)
        {
            SpawnGhostRoles(rule.GameRule, rule.Definition, rule.Count);
        }
    }

    /// <summary>
    /// Takes a list of AntagCounts and tries to make ghost roles out of them
    /// </summary>
    /// <param name="gameRule">Game rule with the associated antags we're spawning</param>
    /// <param name="antagRules">Antags we want to make into ghost roles, with paired counts we need to spawn</param>
    [PublicAPI]
    public void SpawnGhostRoles(Entity<AntagSelectionComponent> gameRule, AntagCount[] antagRules)
    {
        foreach (var rule in antagRules)
        {
            SpawnGhostRoles(gameRule, rule.Definition, rule.Count);
        }
    }

    /// <inheritdoc cref="SpawnGhostRoles(Entity{AntagSelectionComponent},AntagSpecifierPrototype,int)"/>
    [PublicAPI]
    public void SpawnGhostRoles(Entity<AntagSelectionComponent> gameRule,
        ProtoId<AntagSpecifierPrototype> protoId,
        int count)
    {
        if (!Proto.Resolve(protoId, out var antag))
            return;

        SpawnGhostRoles(gameRule, antag, count);
    }

    /// <summary>
    /// Creates ghost role spawners for a given antag definition equivalent to the count.
    /// </summary>
    /// <param name="gameRule">Game rule with the associated antags we're spawning</param>
    /// <param name="proto">Antag prototype we're spawning.</param>
    /// <param name="count">Amount of ghost roles we are spawning.</param>
    [PublicAPI]
    public void SpawnGhostRoles(Entity<AntagSelectionComponent> gameRule, AntagSpecifierPrototype proto, int count)
    {
        for (var i = 0; i < count; i++)
        {
            SpawnGhostRole(gameRule, proto);
        }
    }

    /// <summary>
    /// Creates a ghost role spawner of a given antag for a given game rule.
    /// </summary>
    /// <param name="gameRule">Game rule with the associated antags we're spawning</param>
    /// <param name="proto">Antag prototype we're spawning.</param>
    [PublicAPI]
    public void SpawnGhostRole(Entity<AntagSelectionComponent> gameRule, AntagSpecifierPrototype proto)
    {
        if (proto.SpawnerPrototype is not { } spawnerPrototype)
            return;

        if (!TryGetValidSpawnPosition(gameRule, proto, out var coordinates))
        {
            Log.Error(
                $"Found no valid positions to place antag spawner for game rule: {ToPrettyString(gameRule)}, antag: {proto.ID}");
            return;
        }

        var spawner = Spawn(spawnerPrototype, coordinates.Value);
        if (!TryComp<GhostRoleAntagSpawnerComponent>(spawner, out var spawnerComp))
        {
            Log.Error($"Antag spawner {spawner} does not have a {nameof(GhostRoleAntagSpawnerComponent)}.");
            _adminLogger.Add(LogType.AntagSelection,
                $"Antag spawner {spawner} in game rule {ToPrettyString(gameRule)} failed due to not having {nameof(GhostRoleAntagSpawnerComponent)}.");
            Del(spawner);
            return;
        }

        spawnerComp.Rule = gameRule;
        spawnerComp.Definition = proto;
    }

    /// <summary>
    /// Attempts to find a valid existing game rule for our antag, creating a new one if none exist.
    /// Then attempts to ticket an existing antag slot to our player, forcing one if there are no open slots.
    /// You shouldn't be using this basically ever except for debug and admin stuff.
    /// </summary>
    [Obsolete]
    public void ForceMakeAntag<T>(ICommonSession player, EntProtoId defaultRule) where T : Component
    {
        var rule = ForceGetGameRuleEnt<T>(defaultRule);

        if (TryAssignNextAvailableAntag(rule, player))
            return;

        if (rule.Comp.Antags.LastOrDefault() is not { } antag || !Proto.Resolve(antag.Proto, out var proto))
            return;

        PreSelectSession(rule, proto, player);
        TryInitializeAntag(rule, proto, player);
    }

    /// <inhereitdoc cref="ForceMakeAntag{T}(ICommonSession,EntProtoId,AntagSpecifierPrototype)"/>
    public void ForceMakeAntag<T>(ICommonSession player, EntProtoId ruleProto, ProtoId<AntagSpecifierPrototype> antagProto) where T : Component
    {
        if (!Proto.Resolve(antagProto, out var antag))
            return;

        ForceMakeAntag<T>(player, ruleProto, antag);
    }

    /// <summary>
    /// Attempts to create a specific antag from a specific game rule prototype. Checking if the game rule already exists first.
    /// </summary>
    /// <param name="player">Player we are making into an antag</param>
    /// <param name="ruleProto">Game rule prototype associated with the antag we are creating.</param>
    /// <param name="proto">Prototype for the antag we are creating.</param>
    /// <typeparam name="T">Component from the game rule we are creating, for faster querying.</typeparam>
    /// <remarks>
    /// Do not use this method for anything other than debugging purposes.
    /// This ignores antag bans and the like so genuinely *do not use this unless it's for debugging purposes*
    /// </remarks>
    public void ForceMakeAntag<T>(ICommonSession player, EntProtoId ruleProto, AntagSpecifierPrototype proto) where T : Component
    {
        var rule = ForceGetGameRuleEnt<T>(ruleProto);

        foreach (var antag in rule.Comp.Antags)
        {
            if (antag.Proto != proto)
                continue;

            // Try and assign this antag, if we fail, then try the next definition!
            PreSelectSession(rule, proto, player);
            if (TryInitializeAntag(rule, proto, player))
                return;
        }

        Log.Error($"Antag Prototype {proto.ID} does not exist in {ToPrettyString(rule)}, {ruleProto}");
    }

    /// <summary>
    /// Tries to find a valid gamerule which matches a specific prototype and component.
    /// Note that this is private because you generally should not be forcing a gamerule and this code is evil.
    /// I'm not touching it any more than I have to.
    /// </summary>
    private Entity<AntagSelectionComponent> ForceGetGameRuleEnt<T>(string id) where T : Component
    {
        var query = EntityQueryEnumerator<T, AntagSelectionComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            if (MetaData(uid).EntityPrototype?.ID == id)
                return (uid, comp);
        }
        var ruleEnt = GameTicker.AddGameRule(id);
        RemComp<LoadMapRuleComponent>(ruleEnt);
        var antag = Comp<AntagSelectionComponent>(ruleEnt);
        antag.AssignmentHandled = true; // don't do normal selection.
        GameTicker.StartGameRule(ruleEnt);
        return (ruleEnt, antag);
    }
}
