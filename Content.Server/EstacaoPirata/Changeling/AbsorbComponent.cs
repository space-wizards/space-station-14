namespace Content.Server.Changeling.Components;

[RegisterComponent]
public sealed class AbsorbComponent : Component
{
    /// <summary>
    /// Whether or not the entity has been absorbed yet.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Absorbed = false;

    /// <summary>
    /// Whether or not a revenant has absorbed this entity
    /// for its DNA yet.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AbsorbComplete = false;
}
