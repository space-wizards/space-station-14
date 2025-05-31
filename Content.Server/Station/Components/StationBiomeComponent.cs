using Content.Server.Station.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Station.Components;

/// <summary>
/// Runs EnsurePlanet against the largest grid on Mapinit.
/// </summary>
[RegisterComponent, Access(typeof(StationBiomeSystem))]
public sealed partial class StationBiomeComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Biome = "BiomeGrasslands";

    // If null, its random
    [DataField]
    public int? Seed = null;

    [DataField]
    public Color MapLightColor = Color.Black;
}
