using Robust.Shared.Random;

namespace Content.Shared.Light.EntitySystems;

using Content.Shared.Light.Components;

public sealed class SharedRandomPointLightSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomPointLightComponent, ComponentStartup>(RandomLight);
    }

    private void RandomLight(Entity<RandomPointLightComponent> ent, ref ComponentStartup args)
    {
        var rpl = ent.Comp;
        var color = new Color(
            _random.NextFloat(0, 1),
            _random.NextFloat(0, 1),
            _random.NextFloat(0, 1));

        _light.SetRadius(ent, _random.NextFloat(rpl.MinRadius, rpl.MaxRadius));
        _light.SetEnergy(ent, _random.NextFloat(rpl.MinEnergy, rpl.MaxEnergy));
        _light.SetColor(ent, color);
    }
}
