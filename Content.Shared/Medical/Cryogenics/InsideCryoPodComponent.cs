using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Cryogenics;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class InsideCryoPodComponent: Component
{
    [ViewVariables]
    [DataField("previousOffset")]
    public Vector2 PreviousOffset { get; set; } = new(0, 0);

    /// <summary>
    /// A modifier for the conductance between the cryo pod and the entity inside.
    /// </summary>
    [DataField]
    public float ConductanceMod = 20f; // Arbitrary number. Likely cryopods will have to be heat containers with special juice in the future, or use Frezon...
}
