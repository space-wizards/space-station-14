namespace Content.Client.SubFloor;

/// <summary>
/// Added clientside if an entity is revealed for TRay.
/// </summary>
[RegisterComponent]
public sealed class TrayRevealedComponent : Component
{
    /// <summary>
    /// If the entity is in range, alpha increases to 1, and if out of range, decreases to 0 and is removed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("alpha")]
    public float Alpha;
}
