using Content.Shared.Roles;

namespace Content.Server.Roles;

[RegisterComponent]
public sealed partial class SuspicionRoleComponent : BaseMindRoleComponent
{
    [ViewVariables]
    public SuspicionRole Role { get; set; } = SuspicionRole.Pending;
}

public static class SusRoleExtensions
{
    public static string GetRoleColor(this SuspicionRole role)
    {
        return role switch
        {
            SuspicionRole.Traitor => "red",
            SuspicionRole.Detective => "blue",
            SuspicionRole.Innocent => "green",
            _ => "white",
        };
    }
}

public enum SuspicionRole
{
    Pending,

    Traitor,
    Detective,
    Innocent,
}
