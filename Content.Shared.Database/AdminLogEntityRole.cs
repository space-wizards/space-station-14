namespace Content.Shared.Database;

/// <summary>
///  Role information for entities in admin logs
/// </summary>
public enum AdminLogEntityRole : byte
{
    Actor = 0,
    Target = 1,
    Tool = 2,
    Victim = 3,
    Container = 4,
    Subject = 6,
    Other = 255
}

