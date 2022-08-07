using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Beam.Components;

public abstract class SharedBeamComponent : Component
{
    [ViewVariables]
    [DataField("bodyPrototype")]
    public string BodyPrototype = "LightningBase";

    /// <summary>
    /// How far should this lightning go?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxLength")]
    public float MaxLength = 5f;
}

[Serializable, NetSerializable]
public sealed class BeamEvent : EntityEventArgs
{
    public Angle Angle;
    public Vector2 CalculatedDistance;
    public EntityCoordinates Offset;
    public Vector2 OffsetCorrection;

    public BeamEvent(Angle angle, Vector2 calculatedDistance, EntityCoordinates offset, Vector2 offsetCorrection)
    {
        Angle = angle;
        CalculatedDistance = calculatedDistance;
        Offset = offset;
        OffsetCorrection = offsetCorrection;
    }
}
