namespace Content.Shared.Lightning.Components;
[RegisterComponent]
public sealed class SharedLightningComponent : Component
{
    /// <summary>
    /// Can this lightning arc to something else?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("canArc")]
    public bool CanArc;

    /// <summary>
    /// How many arcs should this produce/how far should it go?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxArc")]
    public int MaxArc;

    /// <summary>
    /// How far should this lightning go?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxLength")]
    public float MaxLength = 5f;
}
