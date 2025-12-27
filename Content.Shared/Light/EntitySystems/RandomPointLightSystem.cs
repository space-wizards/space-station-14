using System.Numerics;
using Robust.Shared.Random;
using Content.Shared.Light.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Light.EntitySystems;

/// <summary>
/// System for assigning random values to <see cref="SharedPointLightComponent"/> variables when given <see cref="RandomPointLightComponent"/>
/// </summary>
public sealed class RandomPointLightSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomPointLightComponent, ComponentStartup>(RandomLight);
    }

    private void RandomLight(Entity<RandomPointLightComponent> ent, ref ComponentStartup args)
    {
        if (_timing.ApplyingState)
            return;

        var rpl = ent.Comp;

        // Keeping value between 0.5 and 1.0 so that it's always bright
        var hsv = new Vector4(
            _random.NextFloat(0, 1),
            _random.NextFloat(0, 1),
            _random.NextFloat(0.5f, 1),
            1);

        var color = Color.FromHsv(hsv);

        _light.SetRadius(ent, _random.NextFloat(rpl.MinRadius, rpl.MaxRadius));
        _light.SetEnergy(ent, _random.NextFloat(rpl.MinEnergy, rpl.MaxEnergy));
        _light.SetColor(ent, color);
    }
}
