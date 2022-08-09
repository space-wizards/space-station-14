using Content.Shared.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Lightning.Components;
public abstract class SharedLightningComponent : Component
{
    /// <summary>
    /// If this can arc, how many targets should this arc to?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxArc")]
    public int MaxArc = 3;

    /// <summary>
    /// List of targets that this collided with already
    /// </summary>
    [ViewVariables]
    [DataField("arcTarget")]
    public EntityUid ArcTarget;

    public int Counter = 0;

    /// <summary>
    /// How far should this lightning go?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxLength")]
    public float MaxLength = 5f;

    /// <summary>
    /// What should this arc to?
    /// </summary>
    [ViewVariables]
    [DataField("collisionMask")]
    public int CollisionMask = (int) (CollisionGroup.MobMask | CollisionGroup.MachineMask);
}
