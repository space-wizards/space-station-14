using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Triggers;
using Robust.Shared.Network;

namespace Content.Shared.Teleportation.Systems;

public sealed partial class SharedTeleportSystem
{
    [Dependency] private INetManager _net = default!;

    /// <summary>
    /// Checks use restrictions without starting a teleport or running effects.
    /// May also be used when collecting verbs.
    /// </summary>
    /// <param name="teleporter">The teleporter being activated.</param>
    /// <param name="target">The entity to teleport.</param>
    /// <param name="user">
    /// The user activating the teleporter, or null for automatic activation. A supplied user must still exist.
    /// </param>
    /// <returns>The cancellation state and optional reason from lifetime checks and teleporter restrictions.</returns>
    public TeleportUseAttemptEvent CheckTeleportUse(EntityUid teleporter, EntityUid target, EntityUid? user)
    {
        var attempt = new TeleportUseAttemptEvent(target, user, Cancelled: true);

        if (TerminatingOrDeleted(teleporter))
            return attempt;

        if (TerminatingOrDeleted(target))
            return attempt;

        if (user != null && TerminatingOrDeleted(user.Value))
            return attempt;

        if (HasComp<TeleportingComponent>(target))
            return attempt;

        attempt.Cancelled = false;
        RaiseLocalEvent(teleporter, ref attempt);
        return attempt;
    }

    /// <summary>
    /// Checks whether the teleporter can be used, then asks its implementation to teleport the target.
    /// Blocks nested teleport requests for the target until all teleport events finish.
    /// </summary>
    /// <remarks>
    /// On clients, the result describes local prediction and does not confirm a server teleport.
    /// </remarks>
    /// <param name="teleporter">The teleporter being activated.</param>
    /// <param name="target">The entity to teleport.</param>
    /// <param name="user">The user activating the teleporter, or null for automatic activation.</param>
    /// <param name="shouldTriggerEffects">Whether to run effects before and after teleportation.</param>
    /// <returns>Whether the teleport implementation reported success.</returns>
    public bool RequestTeleport(
        EntityUid teleporter,
        EntityUid target,
        EntityUid? user,
        bool shouldTriggerEffects = true)
    {
        var attempt = CheckTeleportUse(teleporter, target, user);
        if (attempt.Cancelled)
            return false;

        try
        {
            AddComp<TeleportingComponent>(target);

            var request = new TeleportRequestEvent(target, user, shouldTriggerEffects);
            RaiseLocalEvent(teleporter, ref request);

            if (request.Handled)
                return request.Succeeded;

            if (_net.IsClient)
                return request.Succeeded;

            Log.Error($"Teleporter {ToPrettyString(teleporter)} couldn't teleport {ToPrettyString(target)} " +
                      "because no teleport implementation handled the request");

            return request.Succeeded;
        }
        finally
        {
            // Immediate removal also tolerates the target or marker being deleted by a handler.
            RemComp<TeleportingComponent>(target);
        }
    }
}
