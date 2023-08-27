namespace Content.Server.Roles;

[ByRefEvent]
public readonly record struct MindGetAllRolesEvent(List<RoleInfo> Roles);

public readonly record struct RoleInfo(Component Component, string Name, bool Antagonist, string? PlayTimeTrackerId = null);
