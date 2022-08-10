using Content.Server.OuterRim.Worldgen.Systems.Overworld;
using Content.Server.OuterRim.Worldgen.Tools;
using Content.Shared.OuterRim.Worldgen.Components;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.OuterRim.Worldgen.PointOfInterest;

public sealed class ScatteredDebrisPoI : PointOfInterestGenerator
{
    [DataField("maps")]
    public List<string> Maps = default!;

    public override void Generate(Vector2i chunk)
    {
        var sampler = IoCManager.Resolve<PoissonDiskSampler>();
        var random = IoCManager.Resolve<IRobustRandom>();
        var mapLoader = IoCManager.Resolve<IMapLoader>();
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var worldChunkSys = entityManager.EntitySysManager.GetEntitySystem<WorldChunkSystem>();

        var density = worldChunkSys.GetChunkDensity(chunk);
        var offs = (int)((WorldChunkSystem.ChunkSize - (density / 2)) / 2);
        var center = chunk * WorldChunkSystem.ChunkSize;
        var topLeft = (-offs, -offs);
        var lowerRight = (offs, offs);
        var debrisPoints = sampler.SampleRectangle(topLeft, lowerRight, density);

        foreach (var point in debrisPoints)
        {
            var (_, grid) = mapLoader.LoadBlueprint(worldChunkSys.WorldMap, random.Pick(Maps), new MapLoadOptions()
            {
                Offset = center + point,
                Rotation = random.NextAngle()
            });
            var comp = entityManager.AddComponent<GridIdentityComponent>(grid!.Value);
            comp.ShowIff = false;
        }


    }
}
