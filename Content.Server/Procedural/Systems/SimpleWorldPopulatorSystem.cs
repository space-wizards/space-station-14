using System.Linq;
using System.Numerics;
using Content.Server.Procedural.Prototypes;
using Content.Server.Procedural.Tools;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Procedural.Systems;

public class SimpleWorldPopulatorSystem : EntitySystem
{
    [Dependency] private readonly PoissonDiskSampler _sampler = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DebrisGenerationSystem _debrisGeneration = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public void SpawnDebrisField(MapCoordinates around, float exclusionZone)
    {
        var radius = _configuration.GetCVar(CCVars.SpawnRadius);
        var separation = _configuration.GetCVar(CCVars.MinDebrisSeparation);
        var layout = _prototypeManager.Index<DebrisLayoutPrototype>(_configuration.GetCVar(CCVars.SpawnDebrisLayout));
        var points = _sampler.SampleCircle(around.Position, radius, separation);
        foreach (var point in points.Where(x => (around.Position - x).Length >= exclusionZone))
        {
            var proto = layout.Pick();
            if (proto is not null)
            {
                var ent = _debrisGeneration.GenerateDebris(proto, new MapCoordinates(point, around.MapId));
                EntityManager.GetComponent<TransformComponent>(ent).WorldRotation = _random.NextAngle();
            }
        }
    }
}
