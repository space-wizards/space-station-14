namespace Content.Server.Administration;

/// <summary>
///     Raised on a player's entity when their subtype is to be displayed on admin interfaces.
///     Can be used to show any text as their subtype.
/// </summary>
/// <remarks>
///     Ideally this should only be used in specific narrow contexts
/// </remarks>
[ByRefEvent]
public struct RoleSubtypeOverrideEvent()
{
    /// <summary>
    ///     If this returns a value, it will be shown on the admin interfaces instead of the mind role's real subtype.
    /// </summary>
    /// <remarks>
    ///     There can only be a single value to be displayed in the end.
    ///     As such, each subscription to this event must be carefully considered, to not conflict with others.
    /// </remarks>
    public LocId? SubtypeOverride = null;
}
