namespace Content.Shared.Lightning.Components;
[RegisterComponent]
public class SharedLightningComponent : Component
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
}
