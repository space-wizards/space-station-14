namespace Content.Server.Ghost.Roles.Components;

/// <summary>
/// Allows a ghost to take over the owning entity. Should not be added to prototypes.
/// </summary>
[RegisterComponent]
[Access(typeof(GhostRoleSystem))]
public sealed partial class GhostTakeoverAvailableComponent : Component;
