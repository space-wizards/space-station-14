using System.Diagnostics;
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
using Robust.Shared.Utility;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    [PublicAPI]
    public bool CanBeAntag(ICommonSession session, AntagRule antagRule, bool checkPref = true)
    {
        return CanBeAntag(session, antagRule.GameRule, antagRule.Definition, checkPref);
    }

    [PublicAPI]
    public bool CanBeAntag(ICommonSession session, Entity<AntagSelectionComponent> gameRule, ProtoId<AntagSpecifierPrototype> proto, bool checkPref = true)
    {
        // Can't be this antag if it doesn't exist :)
        if (!Proto.Resolve(proto, out var antag))
            return false;

        return CanBeAntag(session, gameRule, antag, checkPref);
    }

    [PublicAPI]
    public bool CanBeAntag(ICommonSession session, Entity<AntagSelectionComponent> gameRule, AntagSpecifierPrototype def, bool checkPref = true)
    {
        if (!IsSessionValid(session, def))
            return false;

        if (gameRule.Comp.PreSelectedSessions.TryGetValue(def, out var preSelected) && preSelected.Contains(session))
            return false;

        // Add player to the appropriate antag pool
        if (checkPref && !TryGetValidAntagPreferences(session, def.PrefRoles))
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

        var bans = _ban.GetAntagBans(session.UserId);
        foreach (var role in def.PrefRoles)
        {
            // We're banned from this antag. Do not pass go.
            if (bans != null && bans.Contains(role))
                return false;
        }

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

        return IsEntityValid(session, def);
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
    public bool IsEntityValid([NotNullWhen(true)]EntityUid? uid, ProtoId<AntagSpecifierPrototype> def)
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
    public bool IsEntityValid([NotNullWhen(true)]EntityUid? uid, AntagSpecifierPrototype def)
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
        var bans = _ban.GetAntagBans(session.UserId);
        if (bans == null)
            return false;

        foreach (var role in definition.PrefRoles)
        {
            // banned!
            if (bans.Contains(role))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to makes a given player into the specified antagonist.
    /// </summary>
    [PublicAPI]
    public bool TryMakeAntag(Entity<AntagSelectionComponent> ent, ICommonSession session, ProtoId<AntagSpecifierPrototype> proto, bool checkPref = true)
    {
        if (!Proto.Resolve(proto, out var def))
            return false;

        return TryMakeAntag(ent, session, def, checkPref);
    }

    /// <summary>
    /// Tries to makes a given player into the specified antagonist.
    /// </summary>
    [PublicAPI]
    public bool TryMakeAntag(Entity<AntagSelectionComponent> ent, ICommonSession session, AntagSpecifierPrototype def, bool checkPref = true)
    {
        _adminLogger.Add(LogType.AntagSelection, $"Start trying to make {session} become the antagonist: {ToPrettyString(ent)}, {def.ID}");

        if (!CanBeAntag(session, ent, def, checkPref))
            return false;

        MakeSessionAntagonist(ent, session, def);
        return true;
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
    public bool TryAssignNextAvailableAntag(Entity<AntagSelectionComponent> gameRule, ICommonSession session, int players)
    {
        foreach (var def in gameRule.Comp.Antags)
        {
            if (!Proto.Resolve(def, out var antag))
                continue;

            // Because this value can theoretically fluctuate as players leave and join, we don't want to cache it.
            if (AllAntagsAssigned(gameRule, antag, players))
                continue;

            // Try and assign this antag, if we fail, then try the next definition!
            if (TryMakeAntag(gameRule, session, def))
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
        // TODO: Make sure you fixed the bug where it spawns too many antags
        // eventually this should probably store the players per definition with some kind of unique identifier.
        // something to figure out later.
        // Sorry buddy, no antag for you!
        if (!RobustRandom.Prob(LateJoinRandomChance))
            return false;

        var query = QueryActiveRules();
        var rules = new List<(EntityUid, AntagSelectionComponent)>();
        while (query.MoveNext(out var uid, out _, out var antag, out _))
        {
            rules.Add((uid, antag));
        }
        RobustRandom.Shuffle(rules);

        var players = GetActivePlayerCount();

        foreach (var (uid, antag) in rules)
        {
            // TODO: We shouldn't need this.
            DebugTools.AssertNotEqual(antag.SelectionTime, AntagSelectionTime.PrePlayerSpawn);

            if (TryAssignNextAvailableAntag((uid, antag), session, players))
                break;
        }

        return false;
    }

    public bool AssignSessionsAntagonist(Entity<AntagSelectionComponent> gameRule, AntagSpecifierPrototype prototype, params ICommonSession[] players)
    {



        // Yay everything worked!!!
        if (!gameRule.Comp.AssignedSessions.TryGetValue(prototype.ID, out var set))
            gameRule.Comp.AssignedSessions.Add(prototype.ID, players.ToHashSet());
        else
            set.UnionWith(players);

        return true;
    }

    /// <summary>
    /// Attempts to find a valid existing game rule for our antag, creating a new one if none exist.
    /// Then attempts to ticket an existing antag slot to our player, forcing one if there are no open slots.
    /// You shouldn't be using this basically ever except for debug and admin stuff.
    /// </summary>
    public void ForceMakeAntag<T>(ICommonSession player, EntProtoId defaultRule) where T : Component
    {
        var rule = ForceGetGameRuleEnt<T>(defaultRule);

        if (!TryAssignNextAvailableAntag(rule, player))
            MakeSessionAntagonist(rule, player, rule.Comp.Antags.LastOrDefault());
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
        antag.AssignmentComplete = true; // don't do normal selection.
        GameTicker.StartGameRule(ruleEnt);
        return (ruleEnt, antag);
    }
}
