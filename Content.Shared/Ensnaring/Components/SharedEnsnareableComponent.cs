namespace Content.Shared.Ensnaring.Components;
/// <summary>
/// Use this on an entity that you would like to be ensnared by anything that has the <see cref="SharedEnsnaringComponent"/>
/// </summary>
public abstract class SharedEnsnareableComponent : Component
{
    /// <summary>
    /// How slow should the ensnared entities walk be?
    /// Data retrieved from the <see cref="EnsnareChangeEvent"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("walkSpeed")]
    public float WalkSpeed = 1.0f;

    /// <summary>
    /// How slow should the ensnared entities sprint be?
    /// Data retrieved from the <see cref="EnsnareChangeEvent"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("sprintSpeed")]
    public float SprintSpeed = 1.0f;

    /// <summary>
    /// Is this entity currently ensnared?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isEnsnared")]
    public bool IsEnsnared;
}
