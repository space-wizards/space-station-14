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
    public int MaxArc;

    /// <summary>
    /// List of targets that this collided with already
    /// </summary>
    [ViewVariables]
    [DataField("arcTargets")]
    public HashSet<EntityUid> ArcTargets = new();

    [ViewVariables]
    [DataField("bodyPrototype", required: true)]
    public string BodyPrototype = default!;

    /// <summary>
    /// How far should this lightning go?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxLength")]
    public float MaxLength = 5f;
}

[Serializable, NetSerializable]
public sealed class LightningEvent : EntityEventArgs
{
    public Angle Angle;
    public Vector2 CalculatedDistance;
    public EntityCoordinates Offset;
    public Vector2 OffsetCorrection;

    public LightningEvent(Angle angle, Vector2 calculatedDistance, EntityCoordinates offset, Vector2 offsetCorrection)
    {
        Angle = angle;
        CalculatedDistance = calculatedDistance;
        Offset = offset;
        OffsetCorrection = offsetCorrection;
    }
}
