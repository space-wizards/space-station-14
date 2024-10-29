namespace Content.Server.Ghost;

/// <summary>
/// This is used to mark Observers properly, as they get Minds
/// </summary>
[RegisterComponent]
public sealed partial class ObserverRoleComponent : Component
{
    public string Name => Loc.GetString("observer-role-name");
}
