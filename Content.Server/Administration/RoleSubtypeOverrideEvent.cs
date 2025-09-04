namespace Content.Server.Administration;

/// <summary>
///     Raised on a player's entity when their subtype is to be displayed on admin interfaces.
///
/// </summary>
/// <param name="SubtypeOverride">The entity that If Override returns a value, that will be used instead of the MindRoleComponent's subtype this action.</param>
/// <remarks>
///     There can only be a single value to be displayed in the end.
///     As such, each subscription to this event must be carefully considered, to not interfere with others.
/// </remarks>
[ByRefEvent]
public struct RoleSubtypeOverrideEvent()
{
    public LocId? SubtypeOverride = null;
}
