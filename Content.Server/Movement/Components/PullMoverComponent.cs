namespace Content.Server.Movement.Components;

/// <summary>
/// Added to an entity that is ctrl-click moving their pulled object.
/// </summary>
/// <remarks>
/// This just exists so we don't have MoveEvent subs going off for every single mob constantly.
/// </remarks>
[RegisterComponent]
public sealed partial class PullMoverComponent : Component
{

}
