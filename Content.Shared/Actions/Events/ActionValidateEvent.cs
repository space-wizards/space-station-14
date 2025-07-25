namespace Content.Shared.Actions.Events;

/// <summary>
/// Raised on an action entity before being used to:
/// 1. Make sure client is sending the correct kind of target (if any)
/// 2. Do any validation on the target, if needed
/// 3. Give the action system an event to raise on the performer, to actually do the action.
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
    /// </summary>
    public bool Invalid;
}
