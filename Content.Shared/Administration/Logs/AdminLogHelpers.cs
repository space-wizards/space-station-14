using Content.Shared.Database;
using Robust.Shared.Player;

namespace Content.Shared.Administration.Logs;

/// <summary>
/// Shared helpers that reduce boilerplate when building player/role data for
/// <see cref="ISharedAdminLogManager.AddStructured"/> calls.
/// </summary>
public static class AdminLogHelpers
{
    /// <summary>
    /// Look up a single actor's session and produce the <c>players</c> / <c>playerRoles</c>
    /// arrays expected by <see cref="ISharedAdminLogManager.AddStructured"/>.
    /// If the entity has no attached session both out-params are left <c>null</c>.
    /// </summary>
    public static void GetActorPlayerData(
        ISharedPlayerManager player,
        EntityUid actor,
        out Guid[]? players,
        out Dictionary<Guid, AdminLogEntityRole>? playerRoles)
    {
        players = null;
        playerRoles = null;

        if (!player.TryGetSessionByEntity(actor, out var session))
            return;

        players = [session.UserId.UserId];
        playerRoles = new Dictionary<Guid, AdminLogEntityRole>
        {
            [session.UserId.UserId] = AdminLogEntityRole.Actor,
        };
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
        players = null;
        playerRoles = null;

        var hasActor = player.TryGetSessionByEntity(actor, out var actorSession);
        var hasTarget = player.TryGetSessionByEntity(target, out var targetSession);

        if (!hasActor && !hasTarget)
            return;

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
