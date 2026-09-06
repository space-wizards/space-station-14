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
    public TeleportUseAttemptEvent CheckTeleportUse(EntityUid teleporter, EntityUid target, EntityUid user)
    {
        var attempt = new TeleportUseAttemptEvent(target, user);

        if (!Exists(teleporter) || TerminatingOrDeleted(teleporter) ||
            !Exists(target) || TerminatingOrDeleted(target) || HasComp<TeleportingComponent>(target))
        {
            attempt.Cancelled = true;
            return attempt;
        }

        RaiseLocalEvent(teleporter, ref attempt);
        return attempt;
    }

    /// <summary>
    /// Checks whether a teleporter can be used, then dispatches a request to its implementation.
    /// The target remains protected from nested requests until all teleport events have finished.
    /// </summary>
    public bool RequestTeleport(EntityUid teleporter, EntityUid target, EntityUid user, bool triggerEffects = true)
    {
        var attempt = CheckTeleportUse(teleporter, target, user);
        if (attempt.Cancelled)
            return false;

        try
        {
            AddComp<TeleportingComponent>(target);

            var request = new TeleportRequestEvent(target, user, triggerEffects);
            RaiseLocalEvent(teleporter, ref request);

            if (!request.Handled && !_net.IsClient)
            {
                Log.Error($"Teleporter {ToPrettyString(teleporter)} couldn't teleport {ToPrettyString(target)} " +
                          "because no teleport implementation handled the request");
            }

            return request.Succeeded;
        }
        finally
        {
            // Immediate removal also tolerates the target or marker being deleted by a handler.
            RemComp<TeleportingComponent>(target);
        }
    }
}
