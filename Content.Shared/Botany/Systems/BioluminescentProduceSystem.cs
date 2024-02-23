using Content.Shared.Botany.Components;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Adds light to produce of bioluminescent plants.
/// Plants themselves do not glow.
/// </summary>
public sealed class BioluminescentProduceSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BioluminescentProduceComponent, PlantCopyTraitsEvent>(OnCopyTraits);
        SubscribeLocalEvent<BioluminescentProduceComponent, ProduceCreatedEvent>(OnProduceCreated);
    }

    // TODO: mutation

    private void OnCopyTraits(Entity<BioluminescentProduceComponent> ent, ref PlantCopyTraitsEvent args)
    {
        var comp = EnsureComp<BioluminescentProduceComponent>(args.Plant);
        comp.Radius = ent.Comp.Radius;
        comp.Color = ent.Comp.Color;
    }

    private void OnProduceCreated(Entity<BioluminescentProduceComponent> ent, ref ProduceCreatedEvent args)
    {
        var light = _light.EnsureLight(ent);
        _light.SetRadius(ent, ent.Comp.Radius, light);
        _light.SetColor(ent, ent.Comp.Color, light);
        _light.SetCastShadows(ent, false, light); // this is expensive, and botanists make lots of plants
    }
}
