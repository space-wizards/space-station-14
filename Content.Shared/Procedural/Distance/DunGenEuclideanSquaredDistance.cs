namespace Content.Shared.Procedural.Distance;

/// <summary>
/// Square distance check from center of noise with some thresholds to compare against.
/// </summary>
public sealed partial class DunGenEuclideanSquaredDistance : IDunGenDistance
{
    [DataField]
    public Vector2i Size;

    public int Width => Size.X;

    public int Height => Size.Y;

    [DataField]
    public float NoiseThreshold = 0.3f;

    [DataField]
    public float DistanceThreshold = 0.4f;

    public Vector2i Size { get; }
}
