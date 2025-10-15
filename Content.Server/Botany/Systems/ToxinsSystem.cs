using Content.Server.Botany.Components;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Handles toxin accumulation and tolerance for plants, applying health damage
/// and decrementing toxins based on per-tick uptake.
/// </summary>
public sealed class ToxinsSystem : PlantGrowthSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToxinsComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<ToxinsComponent> ent, ref OnPlantGrowEvent args)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        PlantHolderComponent? holder = null;
        Resolve(uid, ref holder);

        if (holder == null || holder.Seed == null || holder.Dead)
            return;

        if (holder.Toxins > 0)
        {
            var toxinUptake = MathF.Max(1, MathF.Round(holder.Toxins / component.ToxinUptakeDivisor));
            if (holder.Toxins > component.ToxinsTolerance)
            {
                holder.Health -= toxinUptake;
            }

            holder.Toxins -= toxinUptake;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }
    }
}
