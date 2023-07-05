using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Parallax;
using Content.Shared.Atmos;
using Content.Shared.Gravity;
using Content.Shared.Parallax.Biomes;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Serilog;

namespace Content.Server._FTL.FTLPoints.Effects;

[DataDefinition]
public sealed class ToPlanetEffect : FTLPointEffect
{
    public override void Effect(FTLPointEffectArgs args)
    {
        var protoManager = IoCManager.Resolve<IPrototypeManager>();
        var random = IoCManager.Resolve<IRobustRandom>();
        var biomeTemplate = random.Pick(protoManager.EnumeratePrototypes<BiomeTemplatePrototype>().ToList());

        var biome = args.EntityManager.EnsureComponent<BiomeComponent>(args.MapUid);
        var biomeSystem = args.EntityManager.System<BiomeSystem>();
        MetaDataComponent? metadata = null;

        biomeSystem.SetSeed(biome, random.Next());
        biomeSystem.SetTemplate(biome, biomeTemplate);
        args.EntityManager.Dirty(biome);

        var gravity = args.EntityManager.EnsureComponent<GravityComponent>(args.MapUid);
        gravity.Enabled = true;
        args.EntityManager.Dirty(gravity, metadata);

        // Day lighting
        // Daylight: #D8B059
        // Midday: #E6CB8B
        // Moonlight: #2b3143
        // Lava: #A34931
        var light = args.EntityManager.EnsureComponent<MapLightComponent>(args.MapUid);
        light.AmbientLightColor = Color.FromHex("#D8B059");
        args.EntityManager.Dirty(light, metadata);

        // Atmos
        var atmos = args.EntityManager.EnsureComponent<MapAtmosphereComponent>(args.MapUid);

        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int) Gas.Oxygen] = 21.824779f;
        moles[(int) Gas.Nitrogen] = 82.10312f;

        var mixture = new GasMixture(2500)
        {
            Temperature = 293.15f,
            Moles = moles,
        };

        args.EntityManager.System<AtmosphereSystem>().SetMapAtmosphere(args.MapUid, false, mixture, atmos);

        args.EntityManager.EnsureComponent<MapGridComponent>(args.MapUid);
    }
}
