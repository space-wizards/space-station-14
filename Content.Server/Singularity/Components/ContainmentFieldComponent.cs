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

    /// <summary>
    /// This shouldn't be at 99999 or higher to prevent the singulo glitching out
    /// Will throw anything at the supplied mass or less that collides with the field.
    /// </summary>
    [ViewVariables]
    [DataField("maxMass")]
    public float MaxMass = 100f;
}
