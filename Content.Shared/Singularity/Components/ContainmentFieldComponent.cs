using Robust.Shared.GameStates;

namespace Content.Shared.Singularity.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ContainmentFieldComponent : Component
{
    /// <summary>
    /// The impulse a containment field applies to an object which collides with it.
    /// At 100 a player moves about a tile at most.
    /// </summary>
    [DataField]
    public float ThrowImpulse = 100f;

    /// <summary>
    /// The maximum speed at which this containment field will throw an object.
    /// 40 should prevent clipping according to sloth...
    /// </summary>
    [DataField]
    public float MaxSpeed = 40f;

    /// <summary>
    /// This shouldn't be at 99999 or higher to prevent the singulo glitching out
    /// Will throw anything at the supplied mass or less that collides with the field.
    /// </summary>
    [DataField]
    public float MaxMass = 10000f;

    /// <summary>
    /// Should field vaporize garbage that collides with it?
    /// </summary>
    [DataField]
    public bool DestroyGarbage = true;
}
