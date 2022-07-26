namespace Content.Shared.Ensnaring.Components;
/// <summary>
/// Use this on an entity that you would like to be ensnared by anything that has the <see cref="SharedEnsnaringComponent"/>
/// </summary>
public abstract class SharedEnsnareableComponent : Component
{
    /// <summary>
    /// Is this entity currently ensnared?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isEnsnared")]
    public bool IsEnsnared;

    public enum EnsnareableVisuals : byte
    {
        NotEnsnared,
        IsEnsnared
    }
}
