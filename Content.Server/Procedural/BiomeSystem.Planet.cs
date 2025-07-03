using Content.Shared.Atmos;
using Content.Shared.Gravity;
using Content.Shared.Light.Components;
using Content.Shared.Procedural.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Procedural;

public sealed partial class BiomeSystem
{
    /// <summary>
    /// Copies the biomecomponent to the specified map.
    /// </summary>
    public BiomeComponent? AddBiome(Entity<BiomeComponent?> mapUid, EntProtoId biomeTemplate, int? seed = null)
    {
        if (!_protomanager.Index(biomeTemplate).Components.TryGetComponent(Factory.GetComponentName<BiomeComponent>(), out var template))
        {
            return null;
        }

        var biome = Factory.GetComponent<BiomeComponent>();
        var biomeObj = (object)biome;
        _serManager.CopyTo(template, ref biomeObj, notNullableOverride: true);
        seed ??= _random.Next();
        biome.Seed = seed.Value;
        AddComp(mapUid, biome, true);
        return biome;
    }

    /// <summary>
    /// Creates a simple planet setup for a map.
    /// </summary>
    public void EnsurePlanet(EntityUid mapUid, EntProtoId biomeTemplate, int? seed = null, MetaDataComponent? metadata = null, Color? mapLight = null)
    {
        if (!Resolve(mapUid, ref metadata))
            return;

        EnsureComp<MapGridComponent>(mapUid);
        AddBiome(mapUid, biomeTemplate, seed);
        var gravity = EnsureComp<GravityComponent>(mapUid);
        gravity.Enabled = true;
        gravity.Inherent = true;
        Dirty(mapUid, gravity, metadata);

        var light = EnsureComp<MapLightComponent>(mapUid);
        light.AmbientLightColor = mapLight ?? Color.FromHex("#D8B059");
        Dirty(mapUid, light, metadata);

        EnsureComp<RoofComponent>(mapUid);

        EnsureComp<LightCycleComponent>(mapUid);

        EnsureComp<SunShadowComponent>(mapUid);
        EnsureComp<SunShadowCycleComponent>(mapUid);

        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int)Gas.Oxygen] = 21.824779f;
        moles[(int)Gas.Nitrogen] = 82.10312f;

        var mixture = new GasMixture(moles, Atmospherics.T20C);

        _atmos.SetMapAtmosphere(mapUid, false, mixture);
    }
}
