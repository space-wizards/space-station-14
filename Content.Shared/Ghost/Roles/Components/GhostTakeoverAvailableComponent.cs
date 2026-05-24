namespace Content.Shared.Ghost.Roles.Components;

/// <summary>
///     Allows a ghost to take over the Owner entity.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedGhostRoleSystem))]
public sealed partial class GhostTakeoverAvailableComponent : Component;
