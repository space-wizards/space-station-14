using Content.Server.Botany.Components;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Applies a death chance and damage to unviable plants each growth tick, updating visuals when necessary.
/// </summary>
public sealed class UnviableGrowthSystem : EntitySystem
{
    [Dependency] private readonly BasicGrowthSystem _basicGrowth = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UnviableGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<UnviableGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        var (plantUid, component) = ent;

        if (TryComp<PlantTraitsComponent>(plantUid, out var traits) && !traits.Viable)
        {
            _basicGrowth.AffectGrowth(plantUid, -1);
            _plantHolder.AdjustsHealth(plantUid, -component.UnviableDamage);
        }
    }
}
