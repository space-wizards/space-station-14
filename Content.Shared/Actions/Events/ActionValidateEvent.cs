namespace Content.Shared.Actions.Events;

/// <summary>
/// Raised on an action entity before it is executed, to:
/// 1. Verify that the client is providing the correct type of target (if applicable)
/// 2. Perform any necessary validation on the target
/// 3. Provide the action system with an event to raise on the performer to carry out the action
/// </summary>
[ByRefEvent]
public struct ActionValidateEvent
{
    /// <summary>
    /// Request event the client sent.
    /// </summary>
    public RequestPerformActionEvent Input;

    /// <summary>
    /// User trying to use the action.
    /// </summary>
    public EntityUid User;

    /// <summary>
    /// Entity providing this action to the user, used for logging.
    /// </summary>
    public EntityUid Provider;

    /// <summary>
    /// If set to true, the client sent invalid event data and this should be logged as an error.
    /// For functioning input that happens to not be allowed this should not be set, for example a range check.
    /// Use <see cref="Cancel"/> instead
    /// </summary>
    public bool Invalid;

    /// <summary>
    /// If set to to true, the Action failed Valdiation and should be Canceld from executing.
    /// </summary>
    public bool Cancel;
}
