using Content.Shared.FixedPoint;

namespace Content.Server.Blob;

[RegisterComponent]
public sealed class BlobResourceComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("pointsPerPulsed")]
    public FixedPoint2 PointsPerPulsed = 3;
}
