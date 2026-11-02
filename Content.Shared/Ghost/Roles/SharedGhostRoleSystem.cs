namespace Content.Shared.Ghost.Roles;

public abstract class SharedGhostRoleSystem : EntitySystem
{
    /// <summary>
    /// Marks all open ghost role UIs for an update.
    /// </summary>
    public virtual void UpdateAllEui() { }
}
