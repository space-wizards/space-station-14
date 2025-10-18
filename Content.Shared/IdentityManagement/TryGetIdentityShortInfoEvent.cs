namespace Content.Shared.IdentityManagement;

/// <summary>
/// Event of attempt to collect actor full title - full name + job from id card for employee or entity name for borgs.
/// </summary>
public sealed class TryGetIdentityShortInfoEvent(EntityUid? whileInteractingWith, EntityUid forActor, bool forLogging = false) : HandledEntityEventArgs
{
    /// <summary>
    /// Full name of <see cref="ForActor"/>, with JobTitle.
    /// Can be null if no system could find actor name / job.
    /// </summary>
    public string? Title;

    /// <summary>
    /// Entity for interacting with which title should be collected.
    /// Could be used to black-out name of people when announcing actions
    /// on e-magged devices.
    /// </summary>
    public readonly EntityUid? WhileInteractingWith = whileInteractingWith;

    /// <summary>
    /// Actor for whom title should be collected.
    /// </summary>
    public readonly EntityUid ForActor = forActor;

    /// <summary>
    /// Marker that title info was requested for access logging.
    /// Is required as event handlers can determine, if they don't need
    /// to place title info due to access logging restrictions.
    /// </summary>
    public readonly bool RequestForAccessLogging = forLogging;
}
