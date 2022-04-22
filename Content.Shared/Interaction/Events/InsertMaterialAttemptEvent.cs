using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared.Interaction.Events;


/// <summary>
///     Raised when a user is trying to insert resource into a lathe
/// </summary>
[PublicAPI]
public sealed class InsertMaterialAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    ///     Entity that triggered the interaction.
    /// </summary>
    public EntityUid User { get; }

    /// <summary>
    ///     The resource
    /// </summary>
    public EntityUid Inserted { get; }

    /// <summary>
    ///     The lathe that the resource is transported into
    /// </summary>
    public EntityUid Target { get; }

    /// <summary>
    ///     The original location that was clicked by the user.
    /// </summary>
    public EntityCoordinates ClickLocation { get; }

    public InsertMaterialAttemptEvent(EntityUid user, EntityUid inserted, EntityUid target, EntityCoordinates clickLocation)
    {
        User = user;
        Inserted = inserted;
        Target = target;
        ClickLocation = clickLocation;
    }
}

