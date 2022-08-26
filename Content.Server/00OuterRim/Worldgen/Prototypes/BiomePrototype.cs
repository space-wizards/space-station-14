using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Server._00OuterRim.Worldgen.Prototypes;

[Prototype("biome")]
public sealed class BiomePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Magic value for people staring at the debug screen.
    /// </summary>
    [DataField("symbol", required: true)]
    public char Symbol { get; } = default!;

    /// <summary>
    /// Lower priority biomes override higher priority ones. For example radioactive ice biome would likely out-prioritize a normal ice biome.
    /// </summary>
    [DataField("priority", required: true)]
    public int Priority { get; } = default!;

    /// <summary>
    /// Layouts this can pick from.
    /// </summary>
    [DataField("debrisLayouts", required: true)]
    public string[] DebrisLayouts { get; } = System.Array.Empty<string>();

    /// <summary>
    /// Temperature ranges for this biome.
    /// </summary>
    [DataField("densityRange")]
    public Range[] DensityRange { get; } =  { new(0.0f, 1.0f ) };

    /// <summary>
    /// Temperature ranges for this biome.
    /// </summary>
    [DataField("temperatureRange")]
    public Range[] TemperatureRange { get; } =  { new(0.0f, 1.0f ) };

    /// <summary>
    /// Radiation ranges for this biome.
    /// </summary>
    [DataField("radiationRange")]
    public Range[] RadiationRange { get; } = { new(0.0f, 1.0f ) };

    /// <summary>
    /// Radiation ranges for this biome.
    /// </summary>
    [DataField("wreckRange")]
    public Range[] WreckRange { get; } = { new(0.0f, 1.0f ) };

    public bool CheckValid(float temperature, float radiation, float wreck, float density)
    {
        return TemperatureRange.Any(x => x.Contains(temperature))
               && RadiationRange.Any(x => x.Contains(radiation))
               && WreckRange.Any(x => x.Contains(wreck))
               && DensityRange.Any(x => x.Contains(density));
    }
}

[DataDefinition]
public readonly struct Range
{
    [DataField("min")]
    public readonly float Min;

    [DataField("max")]
    public readonly float Max;

    public Range(float min, float max)
    {
        Min = min;
        Max = max;
    }

    public bool Intersects(Range other)
    {
        return Min <= other.Max && Max <= other.Min;
    }

    public bool Contains(float num)
    {
        return num >= Min && num <= Max;
    }
}
