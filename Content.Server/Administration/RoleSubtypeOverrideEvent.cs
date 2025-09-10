namespace Content.Server.Administration;

/// <summary>
///     Raised on a player's entity when their subtype is to be displayed on admin interfaces.
///     Can be used to conditionally override the MindRole's subtype.
/// </summary>
[ByRefEvent]
public struct RoleSubtypeOverrideEvent()
{
    /// <summary>
    ///     If this returns a value it will be displayed as the subtype.
    /// </summary>
    /// <remarks>
    ///     There can only be a single value to be displayed in the end.
    ///     As such, each subscription to this event must be carefully considered, to not conflict with others.
    /// </remarks>
    public LocId? SubtypeOverride = null;
}
