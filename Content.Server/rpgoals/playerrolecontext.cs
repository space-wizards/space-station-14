namespace Content.Server.RPGoals;

public sealed record PlayerRoleContext(
    string UserId,
    string RoleId,
    string? Department,
    IReadOnlySet<string> JobTags,
    int RoundMinutes
);
