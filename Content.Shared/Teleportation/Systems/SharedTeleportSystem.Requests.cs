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
    /// <param name="user">The explicit initiator, or null for automatic activation. A supplied initiator must still exist.</param>
    public TeleportUseAttemptEvent CheckTeleportUse(EntityUid teleporter, EntityUid target, EntityUid? user)
    {
        var attempt = new TeleportUseAttemptEvent(target, user, Cancelled: true);

        if (!Exists(teleporter))
            return attempt;

        if (TerminatingOrDeleted(teleporter))
            return attempt;

        if (!Exists(target))
            return attempt;

        if (TerminatingOrDeleted(target))
            return attempt;

        if (user != null && !Exists(user.Value))
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
    /// Checks whether a teleporter can be used, then dispatches a request to its implementation.
    /// The target remains protected from nested requests until all teleport events have finished.
    /// </summary>
    /// <param name="teleporter">The teleporter being activated.</param>
    /// <param name="target">The entity to teleport.</param>
    /// <param name="user">The explicit initiator, or null for automatic activation.</param>
    /// <param name="triggerEffects">Whether to run effects before and after movement.</param>
    public bool RequestTeleport(EntityUid teleporter, EntityUid target, EntityUid? user, bool triggerEffects = true)
    {
        var attempt = CheckTeleportUse(teleporter, target, user);
        if (attempt.Cancelled)
            return false;

        try
        {
            AddComp<TeleportingComponent>(target);

            var request = new TeleportRequestEvent(target, user, triggerEffects);
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
