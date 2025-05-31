using Robust.Shared.Noise;

namespace Content.Shared.Procedural.DungeonLayers;

/// <summary>
/// Sets tiles as rooved.
/// </summary>
public sealed partial class RoofDunGen : IDunGenLayer
{
    [DataField]
    public float Threshold = -1f;

    [DataField]
    public FastNoiseLite? Noise;
}
