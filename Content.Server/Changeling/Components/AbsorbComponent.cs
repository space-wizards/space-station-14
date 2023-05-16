namespace Content.Server.Changeling.Components;

[RegisterComponent]
public sealed class AbsorbComponent : Component
{
    /// <summary>
    /// Whether or not the entity has been absorbed yet.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Absorbed = false;
}
