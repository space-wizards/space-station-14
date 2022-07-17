using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedContainmentFieldComponent))]
public sealed class ContainmentFieldComponent : SharedContainmentFieldComponent
{
    /// <summary>
    /// The throw force for the field if a player collides with it
    /// </summary>
    [ViewVariables]
    [DataField("throwForce")]
    public float ThrowForce = 100f;
}
