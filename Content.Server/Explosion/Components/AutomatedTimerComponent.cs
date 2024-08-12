namespace Content.Server.Explosion.Components;

/// <summary>
///     Disallows starting the timer by hand, must be stuck or triggered by a system using <c>StartTimer</c>.
/// </summary>
[RegisterComponent]
public sealed partial class AutomatedTimerComponent : Component
{
}
