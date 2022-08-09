namespace Content.Server.Ghost.Roles.Components;

/// <summary>
/// This is used to mark that an entity is being used for a ghost role group.
/// </summary>
[RegisterComponent]
[Access(typeof(GhostRoleGroupSystem))]
public sealed class GhostRoleGroupComponent : Component
{
    public uint Identifier { get; set; }
}
