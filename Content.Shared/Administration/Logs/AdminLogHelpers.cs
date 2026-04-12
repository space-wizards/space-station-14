using System.Linq;
using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Administration.Logs;

/// <summary>
/// Explicit Tier 3 semantic data:
/// This is the shared exception-path bundle for the cases where the message and payload are not enough.
/// It keeps <c>players</c>, <c>entities</c>, and <c>playerRoles</c> together so call sites do not have to
/// rebuild them by hand.
/// </summary>
public readonly record struct AdminLogExplicitSemantics(
    IReadOnlyCollection<Guid>? Players = null,
    IReadOnlyCollection<AdminLogEntityRef>? Entities = null,
    IReadOnlyDictionary<Guid, AdminLogEntityRole>? PlayerRoles = null);

/// <summary>
/// Shared helpers for the explicit Tier 3 path:
/// Tier 1 and Tier 2 should still be the default. These helpers are for the smaller set of logs where
/// explicit participant definitions are needed
/// </summary>
public static class AdminLogHelpers
{
    /// <summary>
    /// Look up a single actor's session and produce the <c>players</c> / <c>playerRoles</c>
    /// If the entity has no attached session both out-params are left <c>null</c>.
    /// </summary>
    public static void GetActorPlayerData(
        ISharedPlayerManager player,
        EntityUid actor,
        out Guid[]? players,
        out Dictionary<Guid, AdminLogEntityRole>? playerRoles)
    {
        var semantics = GetActorSemantics(player, actor);
        players = semantics.Players as Guid[];
        playerRoles = semantics.PlayerRoles as Dictionary<Guid, AdminLogEntityRole>;
    }

    /// <summary>
    /// Builds explicit semantics for a single actor, optionally with an explicit tool entity.
    /// This is the shared Tier 3 path for the actor + tool family when one entity causes or uses
    /// another entity but there is no separate target/victim/subject family to represent.
    /// Prefer semantic interpolation instead of this helper unless you truly need explicit Tier 3
    /// participant data.
    /// </summary>
    public static AdminLogExplicitSemantics GetActorSemantics(
        ISharedPlayerManager player,
        EntityUid actor,
        EntityUid? tool = null,
        AdminLogEntityRole toolRole = AdminLogEntityRole.Tool)
    {
        Guid[]? players = null;
        Dictionary<Guid, AdminLogEntityRole>? playerRoles = null;

        if (player.TryGetSessionByEntity(actor, out var session))
        {
            players = [session.UserId.UserId];
            playerRoles = new Dictionary<Guid, AdminLogEntityRole>
            {
                [session.UserId.UserId] = AdminLogEntityRole.Actor,
            };
        }

        var entities = tool == null
            ? [new AdminLogEntityRef(actor, AdminLogEntityRole.Actor)]
            : new[]
            {
                new AdminLogEntityRef(actor, AdminLogEntityRole.Actor),
                new AdminLogEntityRef(tool.Value, toolRole),
            };

        return new AdminLogExplicitSemantics(players, entities, playerRoles);
    }

    /// <summary>
    /// Builds explicit semantics from a known player session, optionally including the player's
    /// attached entity under a supplied semantic role.
    /// This is intended for player-backed logs where the player must remain queryable even if the
    /// attached entity is absent or invalid.
    /// </summary>
    public static AdminLogExplicitSemantics GetSessionSemantics(
        ICommonSession session,
        AdminLogEntityRole? playerRole = null,
        AdminLogEntityRole? attachedEntityRole = null)
    {
        var players = new[] { session.UserId.UserId };
        Dictionary<Guid, AdminLogEntityRole>? playerRoles = null;

        if (playerRole != null)
        {
            playerRoles = new Dictionary<Guid, AdminLogEntityRole>
            {
                [session.UserId.UserId] = playerRole.Value,
            };
        }

        IReadOnlyCollection<AdminLogEntityRef>? entities = null;
        if (attachedEntityRole != null && session.AttachedEntity is { Valid: true } attached)
        {
            entities = [new AdminLogEntityRef(attached, attachedEntityRole.Value)];
        }

        return new AdminLogExplicitSemantics(players, entities, playerRoles);
    }

    /// <summary>
    /// Look up two participants (actor + target/victim) and produce combined
    /// <c>players</c> / <c>playerRoles</c> arrays.
    /// Handles the case where actor == target (self-action) by deduplicating.
    /// </summary>
    public static void GetActorTargetPlayerData(
        ISharedPlayerManager player,
        EntityUid actor,
        EntityUid target,
        AdminLogEntityRole targetRole,
        out Guid[]? players,
        out Dictionary<Guid, AdminLogEntityRole>? playerRoles)
    {
        var semantics = GetActorTargetSemantics(player, actor, target, targetRole);
        players = semantics.Players as Guid[];
        playerRoles = semantics.PlayerRoles as Dictionary<Guid, AdminLogEntityRole>;
    }

    /// <summary>
    /// Convenience wrapper for the common actor + victim player-data case.
    /// Self-actions collapse to a single Actor player role.
    /// </summary>
    public static void GetActorVictimPlayerData(
        ISharedPlayerManager player,
        EntityUid actor,
        EntityUid victim,
        out Guid[]? players,
        out Dictionary<Guid, AdminLogEntityRole>? playerRoles)
    {
        GetActorTargetPlayerData(player, actor, victim, AdminLogEntityRole.Victim, out players, out playerRoles);
    }

    /// <summary>
    /// Builds explicit semantics for an actor plus target/victim and an optional tool.
    /// When <paramref name="actor"/> and <paramref name="target"/> resolve to the same player,
    /// the player-role map intentionally keeps the Actor role while the explicit entity refs preserve
    /// the richer dual-role entity participation. This matches the current storage model, where a
    /// single player GUID can only store one role, while entity participation can still reflect both
    /// sides of a self-action.
    /// </summary>
    public static AdminLogExplicitSemantics GetActorTargetSemantics(
        ISharedPlayerManager player,
        EntityUid actor,
        EntityUid target,
        AdminLogEntityRole targetRole,
        EntityUid? tool = null,
        AdminLogEntityRole toolRole = AdminLogEntityRole.Tool)
    {
        Guid[]? players = null;
        Dictionary<Guid, AdminLogEntityRole>? playerRoles = null;

        var hasActor = player.TryGetSessionByEntity(actor, out var actorSession);
        var hasTarget = player.TryGetSessionByEntity(target, out var targetSession);

        if (!hasActor && !hasTarget)
            return new AdminLogExplicitSemantics(
                Entities: tool == null
                    ? [new AdminLogEntityRef(actor, AdminLogEntityRole.Actor), new AdminLogEntityRef(target, targetRole)]
                    :
                    [
                        new AdminLogEntityRef(actor, AdminLogEntityRole.Actor),
                        new AdminLogEntityRef(target, targetRole),
                        new AdminLogEntityRef(tool.Value, toolRole),
                    ]);

        playerRoles = new Dictionary<Guid, AdminLogEntityRole>();

        if (hasActor && hasTarget)
        {
            var actorGuid = actorSession!.UserId.UserId;
            var targetGuid = targetSession!.UserId.UserId;

            if (actorGuid == targetGuid)
            {
                // Self-action — single entry with Actor role.
                players = [actorGuid];
                playerRoles[actorGuid] = AdminLogEntityRole.Actor;
            }
            else
            {
                players = [actorGuid, targetGuid];
                playerRoles[actorGuid] = AdminLogEntityRole.Actor;
                playerRoles[targetGuid] = targetRole;
            }
        }
        else if (hasActor)
        {
            var actorGuid = actorSession!.UserId.UserId;
            players = [actorGuid];
            playerRoles[actorGuid] = AdminLogEntityRole.Actor;
        }
        else
        {
            var targetGuid = targetSession!.UserId.UserId;
            players = [targetGuid];
            playerRoles[targetGuid] = targetRole;
        }

        var entities = new List<AdminLogEntityRef>(tool == null ? 2 : 3)
        {
            new(actor, AdminLogEntityRole.Actor),
            new(target, targetRole),
        };

        if (tool != null)
            entities.Add(new AdminLogEntityRef(tool.Value, toolRole));

        return new AdminLogExplicitSemantics(players, entities, playerRoles);
    }

    /// <summary>
    /// Convenience wrapper for the common actor + victim + tool explicit Tier 3 case.
    /// In self-action cases, entity refs preserve both Actor and Victim participation while the
    /// player-role map intentionally collapses to a single Actor role for the shared player GUID.
    /// Use this when a self-action or other awkward dual-role event would otherwise force each call
    /// site to manually rebuild the same explicit semantics.
    /// </summary>
    public static AdminLogExplicitSemantics GetActorVictimToolSemantics(
        ISharedPlayerManager player,
        EntityUid actor,
        EntityUid victim,
        EntityUid tool)
    {
        return GetActorTargetSemantics(player, actor, victim, AdminLogEntityRole.Victim, tool);
    }

    /// <summary>
    /// Builds explicit semantics for an actor, a subject entity, and a target/victim entity,
    /// optionally with a tool. This is a good fit for projectile-style logs where the projectile
    /// itself should remain queryable as the subject of the action.
    /// </summary>
    public static AdminLogExplicitSemantics GetActorSubjectTargetSemantics(
        ISharedPlayerManager player,
        EntityUid actor,
        EntityUid subject,
        EntityUid target,
        AdminLogEntityRole targetRole,
        EntityUid? tool = null,
        AdminLogEntityRole toolRole = AdminLogEntityRole.Tool)
    {
        Guid[]? players = null;
        Dictionary<Guid, AdminLogEntityRole>? playerRoles = null;

        if (player.TryGetSessionByEntity(actor, out var actorSession))
        {
            players = [actorSession.UserId.UserId];
            playerRoles = new Dictionary<Guid, AdminLogEntityRole>
            {
                [actorSession.UserId.UserId] = AdminLogEntityRole.Actor,
            };
        }

        var entities = new List<AdminLogEntityRef>(tool == null ? 3 : 4)
        {
            new(actor, AdminLogEntityRole.Actor),
            new(subject, AdminLogEntityRole.Subject),
            new(target, targetRole),
        };

        if (tool != null)
            entities.Add(new AdminLogEntityRef(tool.Value, toolRole));

        return new AdminLogExplicitSemantics(players, entities, playerRoles);
    }

    /// <summary>
    /// Convenience wrapper for the common actor + subject + victim (+ optional tool) explicit Tier 3 case.
    /// </summary>
    public static AdminLogExplicitSemantics GetActorSubjectVictimSemantics(
        ISharedPlayerManager player,
        EntityUid actor,
        EntityUid subject,
        EntityUid victim,
        EntityUid? tool = null)
    {
        return GetActorSubjectTargetSemantics(player, actor, subject, victim, AdminLogEntityRole.Victim, tool);
    }

    /// <summary>
    /// Builds explicit semantics for one actor and many victims, optionally with a tool.
    /// This preserves every victim at the entity layer while recording each distinct player GUID
    /// once in the player-role map. If the actor is also present in the victim set, the player-role
    /// entry intentionally remains <see cref="AdminLogEntityRole.Actor"/> for that shared GUID.
    /// </summary>
    public static AdminLogExplicitSemantics GetActorVictimsSemantics(
        ISharedPlayerManager player,
        EntityUid actor,
        IReadOnlyCollection<EntityUid> victims,
        EntityUid? tool = null,
        AdminLogEntityRole toolRole = AdminLogEntityRole.Tool)
    {
        Guid[]? players = null;
        Dictionary<Guid, AdminLogEntityRole>? playerRoles = null;

        if (player.TryGetSessionByEntity(actor, out var actorSession))
        {
            var actorGuid = actorSession.UserId.UserId;
            players = [actorGuid];
            playerRoles = new Dictionary<Guid, AdminLogEntityRole>
            {
                [actorGuid] = AdminLogEntityRole.Actor,
            };
        }

        foreach (var victim in victims)
        {
            if (!player.TryGetSessionByEntity(victim, out var victimSession))
                continue;

            var victimGuid = victimSession.UserId.UserId;

            if (players == null)
            {
                players = [victimGuid];
                playerRoles = new Dictionary<Guid, AdminLogEntityRole>
                {
                    [victimGuid] = AdminLogEntityRole.Victim,
                };
                continue;
            }

            if (!players.Contains(victimGuid))
                players = [.. players, victimGuid];

            playerRoles ??= new Dictionary<Guid, AdminLogEntityRole>();
            playerRoles.TryAdd(victimGuid, AdminLogEntityRole.Victim);
        }

        var entities = new List<AdminLogEntityRef>(victims.Count + (tool == null ? 1 : 2))
        {
            new(actor, AdminLogEntityRole.Actor),
        };

        if (tool != null)
            entities.Add(new AdminLogEntityRef(tool.Value, toolRole));

        entities.AddRange(victims.Select(victim => new AdminLogEntityRef(victim, AdminLogEntityRole.Victim)));

        return new AdminLogExplicitSemantics(players, entities, playerRoles);
    }

    /// <summary>
    /// Convenience wrapper for the common actor + many victims + tool explicit Tier 3 case.
    /// </summary>
    public static AdminLogExplicitSemantics GetActorVictimsToolSemantics(
        ISharedPlayerManager player,
        EntityUid actor,
        IReadOnlyCollection<EntityUid> victims,
        EntityUid tool)
    {
        return GetActorVictimsSemantics(player, actor, victims, tool);
    }

    /// <summary>
    /// Build player data from an already-known session (e.g. voting, game ticker).
    /// Use when you have a <see cref="Guid"/> player ID but no <see cref="EntityUid"/>.
    /// </summary>
    public static void GetPlayerData(
        Guid playerId,
        AdminLogEntityRole role,
        out Guid[] players,
        out Dictionary<Guid, AdminLogEntityRole> playerRoles)
    {
        players = [playerId];
        playerRoles = new Dictionary<Guid, AdminLogEntityRole>
        {
            [playerId] = role,
        };
    }
}
