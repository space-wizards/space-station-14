namespace Content.Shared.Database;

/// <summary>
/// Types of bans that can be stored in the database.
/// </summary>
public enum BanType : byte
{
    /// <summary>
    /// A ban from the entire server. If a player matches the ban info, they will be refused connection.
    /// </summary>
    Server,

    /// <summary>
    /// A ban from playing one or more roles.
    /// </summary>
    Role,
}

/// <summary>
/// A single role for a database role ban.
/// </summary>
/// <param name="RoleType">The type of role being banned, e.g. <c>Job</c>.</param>
/// <param name="RoleId">
/// The ID of the role being banned. This is likely a prototype ID based on <paramref name="RoleType"/>.
/// </param>
[Serializable]
public record struct BanRoleDef(string RoleType, string RoleId)
{
    public override string ToString()
    {
        return $"{RoleType}:{RoleId}";
    }
}
