using System.Linq;
using Content.Server._00OuterRim.Worldgen.Prototypes;
using Content.Server._00OuterRim.Worldgen.Tools;

namespace Content.Server._00OuterRim.Worldgen.Systems.Overworld;

public partial class WorldChunkSystem
{
    private Dictionary<int, List<BiomePrototype>> _biomes = new();
    private List<int> _biomePrioritiesSorted = new();

    // Density
    private const float DensityControllerCoordinateScale = 6f;
    private const float MinDensity = 90.0f;
    private const float MaxDensity = 40.0f;
    private const float DensityScale = MinDensity - MaxDensity;
    private const float DensityClipPointMin = 0.4f;
    private const float DensityClipPointMax = 0.5f;

    // Radiation
    private const float RadiationControllerCoordinateScale = 16f;
    private const float RadiationStormMinimum = 0.7f;
    private const float RadiationMinimum = 0.6f;

    // Temperature
    private const float TemperatureControllerCoordinateScale = 6f;
    private const float RadiationHotspotMin = 0.5f;
    private float RadiationHotspotScale(float inp)
        => Math.Min(RadiationHotspotMin + inp * (1.0f - RadiationHotspotMin) - 0.3f, RadiationHotspotMin);

    private const float WreckControllerCoordinateScale = 16f;

    private FastNoise _densityController = default!;
    private FastNoise _radiationController = default!;
    private FastNoise _temperatureController = default!;
    private FastNoise _wreckController = default!;

    private void InitBiomeCache()
    {
        foreach (var biome in _prototypeManager.EnumeratePrototypes<BiomePrototype>())
        {
            if (!_biomes.ContainsKey(biome.Priority))
                _biomes.Add(biome.Priority, new List<BiomePrototype>());

            _biomes[biome.Priority].Add(biome);
        }

        _biomePrioritiesSorted = _biomes.Keys.ToList();
        _biomePrioritiesSorted.Sort();
    }

    private IEnumerable<BiomePrototype> BiomesByPriority()
    {
        foreach (var priority in _biomePrioritiesSorted)
        {
            foreach (var biome in _biomes[priority])
            {
                yield return biome;
            }
        }
    }

    private void ResetNoise()
    {
        _densityController = new FastNoise();
        _densityController.SetSeed(_random.Next());
        _densityController.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
        _densityController.SetFractalType(FastNoise.FractalType.FBM);
        _densityController.SetFractalLacunarity((float) (Math.PI * 2 / 3));

        _radiationController = new FastNoise();
        _radiationController.SetSeed(_random.Next());
        _radiationController.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
        _radiationController.SetFractalType(FastNoise.FractalType.RigidMulti);
        _radiationController.SetFractalLacunarity((float) Math.PI * 1 / 5);

        _temperatureController = new FastNoise();
        _temperatureController.SetSeed(_random.Next());
        _temperatureController.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
        _temperatureController.SetFractalType(FastNoise.FractalType.FBM);
        _temperatureController.SetFractalLacunarity((float) (Math.PI * 2 / 3));

        _wreckController = new FastNoise();
        _wreckController.SetSeed(_random.Next());
        _wreckController.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
        _wreckController.SetFractalType(FastNoise.FractalType.FBM);
        _wreckController.SetFractalLacunarity((float) (Math.PI * 2 / 3));
    }

    #region Density
    private float GetDensityValue(Vector2i chunk)
    {
        var scaled = chunk * DensityControllerCoordinateScale;
        return (_densityController.GetPerlin(scaled.X, scaled.Y) + 1) / 2; // Scale it to be between 0 and 1.
    }

    public float GetChunkDensity(Vector2i chunk)
    {
        return MaxDensity + (GetDensityValue(chunk) * DensityScale);
    }

    private bool ShouldClipChunk(Vector2i chunk)
    {
        var density = GetDensityValue(chunk);
        return density is > DensityClipPointMin and < DensityClipPointMax;
    }

    private float GetDensityClipped(Vector2i chunk)
    {
        return ShouldClipChunk(chunk) ? 0.0f : GetDensityValue(chunk);
    }
    #endregion

    #region Radiation
    private float GetRadiationValue(Vector2i chunk)
    {
        var scaled = chunk * RadiationControllerCoordinateScale;
        return (_radiationController.GetPerlin(scaled.X, scaled.Y) + 1) / 2; // Scale it to be between 0 and 1.
    }

    private bool ShouldRadstorm(Vector2i chunk)
    {
        var rad = GetRadiationValue(chunk);
        return rad is > RadiationStormMinimum && !ShouldClipChunk(chunk);
    }

    private float GetRadiationClipped(Vector2i chunk)
    {
        var rad = GetRadiationValue(chunk);
        if (rad < RadiationMinimum || ShouldClipChunk(chunk))
            return 0.0f;
        return (rad - RadiationMinimum) / (1 - RadiationMinimum);
    }
    #endregion

    #region Temperature

    private float GetTemperatureValue(Vector2i chunk)
    {
        var scaled = chunk * TemperatureControllerCoordinateScale;
        return (_temperatureController.GetPerlin(scaled.X, scaled.Y) + 1) / 2; // Scale it to be between 0 and 1.
    }

    private float GetTemperatureClipped(Vector2i chunk)
    {
        if (ShouldClipChunk(chunk))
            return 0.0f;
        return GetRadiationClipped(chunk) > 0.0f ? RadiationHotspotScale(GetTemperatureValue(chunk)) : GetTemperatureValue(chunk);
    }
    #endregion

    #region Wreck Concentration
    private float GetWreckValue(Vector2i chunk)
    {
        var scaled = chunk * WreckControllerCoordinateScale;
        return (_wreckController.GetPerlin(scaled.X, scaled.Y) + 1) / 2; // Scale it to be between 0 and 1.
    }

    private float GetWreckClipped(Vector2i chunk)
    {
        return ShouldClipChunk(chunk) ? 0.0f : GetWreckValue(chunk);
    }
    #endregion


    private BiomePrototype SelectBiome(Vector2i chunk)
    {
        foreach (var biome in BiomesByPriority())
        {
            if (biome.CheckValid(GetTemperatureClipped(chunk), GetRadiationClipped(chunk), GetWreckClipped(chunk), GetDensityClipped(chunk)))
                return biome;
        }

        throw new Exception("Couldn't select a biome.");
    }
}
