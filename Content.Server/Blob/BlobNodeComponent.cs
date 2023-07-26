using Content.Shared.FixedPoint;

namespace Content.Server.Blob;

[RegisterComponent]
public sealed class BlobNodeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("pulseFrequency")]
    public FixedPoint2 PulseFrequency = 5;

    [ViewVariables(VVAccess.ReadWrite), DataField("pulseRadius")]
    public float PulseRadius = 2f;

    public float Accumulator = 0;
}

public sealed class BlobTileGetPulseEvent : EntityEventArgs
{
    public bool Explain { get; set; }
}

public sealed class BlobMobGetPulseEvent : EntityEventArgs
{
}
