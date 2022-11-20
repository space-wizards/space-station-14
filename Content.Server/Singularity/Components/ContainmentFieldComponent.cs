using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedContainmentFieldComponent))]
public sealed class ContainmentFieldComponent : SharedContainmentFieldComponent
{
    /// <summary>
    /// The throw force for the field if an entity collides with it
    /// The lighter the mass the further it will throw. 5 mass will go about 4 tiles out, 70 mass goes only a couple tiles.
    /// </summary>
    [DataField("throwForce")]
    public float ThrowForce = 100f;

    /// <summary>
    /// This shouldn't be at 99999 or higher to prevent the singulo glitching out
    /// Will throw anything at the supplied mass or less that collides with the field.
    /// </summary>
    [DataField("maxMass")]
    public float MaxMass = 10000f;
}
