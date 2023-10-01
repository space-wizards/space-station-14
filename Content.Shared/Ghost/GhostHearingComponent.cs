namespace Content.Shared.Ghost;

/// <summary>
/// This is used for marking entities which should receive all local chat message, even when out of range
/// </summary>
[RegisterComponent]
public sealed partial class GhostHearingComponent : Component
{
}
