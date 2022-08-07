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

    [ViewVariables]
    [DataField("bodyPrototype")]
    public string BodyPrototype = "LightningBase";

    /// <summary>
    /// How far should this lightning go?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxLength")]
    public float MaxLength = 5f;

    public int Counter = 0;
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
