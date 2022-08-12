using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Beam.Components;

public abstract class SharedBeamComponent : Component
{
    /// <summary>
    /// A unique list of targets that this beam collided with.
    /// Useful for code like Arcing in the Lightning Component.
    /// </summary>
    [ViewVariables]
    [DataField("hitTargets")]
    public HashSet<EntityUid> HitTargets = new();

    /// <summary>
    /// The virtual entity representing a beam.
    /// </summary>
    [ViewVariables]
    [DataField("virtualBeamController")]
    public EntityUid? VirtualBeamController;
}
