using Content.Server.Botany.Components;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Applies a death chance and damage to unviable plants each growth tick, updating visuals when necessary.
/// </summary>
public sealed class UnviableGrowthSystem : PlantGrowthSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnviableGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<UnviableGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        PlantHolderComponent? holder = null;
        Resolve(uid, ref holder);

        if (holder?.Seed == null || holder.Dead)
            return;

        // Unviable plants have a chance to die each growth cycle
        if (_random.Prob(component.DeathChance))
        {
            holder.Health -= component.DeathDamage;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }
    }
}
