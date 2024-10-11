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
    public static Color GetRoleColor(this SuspicionRole role)
    {
        return role switch
        {
            SuspicionRole.Traitor => Color.Red,
            SuspicionRole.Detective => Color.Blue,
            SuspicionRole.Innocent => Color.Green,
            _ => Color.Gray,
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
