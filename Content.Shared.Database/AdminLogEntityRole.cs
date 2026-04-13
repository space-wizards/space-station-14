namespace Content.Shared.Database;

/// <summary>
/// The role an entity played in an admin log event.
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

