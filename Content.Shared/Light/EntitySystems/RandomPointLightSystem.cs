using System.Numerics;
using Content.Shared.Light.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Light.EntitySystems;

/// <summary>
/// System for assigning random values to <see cref="SharedPointLightComponent"/> variables when given <see cref="RandomPointLightComponent"/>
/// </summary>
public sealed class RandomPointLightSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
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

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        // Keeping the V variable between 0.5 and 1.0 so that it's always bright
        var hsv = new Vector4(
            rand.NextFloat(0, 1),
            rand.NextFloat(0, 1),
            rand.NextFloat(0.5f, 1),
            1);

        var color = Color.FromHsv(hsv);

        _light.SetRadius(ent, rand.NextFloat(rpl.MinRadius, rpl.MaxRadius));
        _light.SetEnergy(ent, rand.NextFloat(rpl.MinEnergy, rpl.MaxEnergy));
        _light.SetColor(ent, color);
    }
}
