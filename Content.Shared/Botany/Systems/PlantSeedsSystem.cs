using Content.Shared.Botany.Components;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Produce gets <see cref="ProduceComponent/>" with a new plant cloned from this one.
/// Becoming seedless via mutation means removing <see cref="PlantSeedsComponent"/>.
/// </summary>
public sealed class PlantSeedsSystem : EntitySystem
{
    [Dependency] private readonly PlantSystem _plant = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantSeedsComponent, PlantCopyTraitsEvent>(OnCopyTraits);
        SubscribeLocalEvent<PlantSeedsComponent, ProduceCreatedEvent>(OnProduceCreated);
    }

    // TODO: seedless mutation

    private void OnCopyTraits(Entity<PlantSeedsComponent> ent, ref PlantCopyTraitsEvent args)
    {
        EnsureComp<PlantSeedsComponent>(args.Plant).Seedless = ent.Comp.Seedless;
    }

    private void OnProduceCreated(Entity<PlantSeedsComponent> ent, ref ProduceCreatedEvent args)
    {
        if (ent.Comp.Seedless)
            return;

        var produce = EnsureComp<ProduceComponent>(args.Produce);
        produce.Seed.Entity = _plant.CreateSeed(ent);
    }
}
