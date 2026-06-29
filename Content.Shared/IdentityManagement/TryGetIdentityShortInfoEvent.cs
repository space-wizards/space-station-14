namespace Content.Shared.IdentityManagement;

/// <summary>
/// Event to collect a full title (full name + job from id card for employee or entity name for borgs).
/// For example "Urist McHands (Captain)" for players or "Robby (SI-123)" for silicons.
/// </summary>
public sealed class TryGetIdentityShortInfoEvent(EntityUid target, EntityUid? whileInteractingWith, bool forLogging) : HandledEntityEventArgs
{
    /// <summary>
    /// Full name of <see cref="ForActor"/>, with JobTitle.
    /// Can be null if no system could find actor name / job.
    /// </summary>
    public string? Title;

    /// <summary>
    /// The entity that was used to get the name information.
    /// Could be used to black-out name of people when announcing actions
    /// on e-magged devices.
    /// </summary>
    public readonly EntityUid? WhileInteractingWith = whileInteractingWith;

    /// <summary>
    /// The entity for which the name should be collected for.
    /// </summary>
    public readonly EntityUid Target = target;

    /// <summary>
    /// Marker that title info was requested for access logging.
    /// Is required as event handlers can determine, if they don't need
    /// to place title info due to access logging restrictions.
    /// </summary>
    /// <remarks>
    /// TODO: This should not be in here. Just check the id card used to access the access reader instead of making the event fail to get the info.
    /// </remarks>
    public readonly bool RequestForAccessLogging = forLogging;
}
