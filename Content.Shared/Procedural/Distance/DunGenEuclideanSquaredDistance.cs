namespace Content.Shared.Procedural.Distance;

/// <summary>
/// Produces a rounder shape useful for more natural areas.
/// </summary>
public sealed partial class DunGenEuclideanSquaredDistance : IDunGenDistance
{
    [DataField]
    public float BlendWeight { get; set; } = 0.50f;
}
