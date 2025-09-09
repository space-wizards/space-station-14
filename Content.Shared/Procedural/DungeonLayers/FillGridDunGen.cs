using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Procedural.Distance;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.DungeonLayers;

/// <summary>
/// Fills unreserved tiles with the specified entity prototype.
/// </summary>
public sealed partial class FillGridDunGen : IDunGenLayer
{
    /// <summary>
    /// Tiles the fill can occur on.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ContentTileDefinition>>? AllowedTiles;

    [DataField(required: true)]
    public EntProtoId Entity;

    #region Noise

    [DataField]
    public bool Invert;

    /// <summary>
    /// Optionally don't spawn entities if the noise value matches.
    /// </summary>
    [DataField]
    public FastNoiseLite? ReservedNoise;

    /// <summary>
    /// Noise threshold for <see cref="ReservedNoise"/>. Does nothing without it.
    /// </summary>
    [DataField]
    public float Threshold = -1f;

    [DataField]
    public IDunGenDistance? DistanceConfig;

    [DataField]
    public Vector2 Size;

    #endregion
}
