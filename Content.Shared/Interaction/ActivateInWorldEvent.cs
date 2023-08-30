using JetBrains.Annotations;

namespace Content.Shared.Interaction;

/// <summary>
///     Raised when an entity is activated in the world.
/// </summary>
[PublicAPI]
public sealed class ActivateInWorldEvent : HandledEntityEventArgs, ITargetedInteractEventArgs
{
    /// <summary>
    ///     Entity that activated the target world entity.
    /// </summary>
    public EntityUid User { get; }

    /// <summary>
    ///     Entity that was activated in the world.
    /// </summary>
    public EntityUid Target { get; }

    /// <summary>
    ///     Set to true when the activation is logged by a specific logger.
    /// </summary>
    public bool WasLogged { get; set; }

    public ActivateInWorldEvent(EntityUid user, EntityUid target)
    {
        User = user;
        Target = target;
    }
}
