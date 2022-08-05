using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Lightning.Components;
public abstract class SharedLightningComponent : Component
{
    /// <summary>
    /// Can this lightning arc to something else?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("canArc")]
    public bool CanArc;

    /// <summary>
    /// How many arcs should this produce/how far should it go?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxArc")]
    public int MaxArc;

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
