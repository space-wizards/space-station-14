using Content.Server.Botany.Components;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Handles toxin accumulation and tolerance for plants, applying health damage
/// and decrementing toxins based on per-tick uptake.
/// </summary>
public sealed class ToxinsSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PlantToxinsComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<PlantToxinsComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        if (!TryComp(uid, out PlantHolderComponent? holder ))
            return;

        if (holder.Toxins < 0)
            return;

        var toxinUptake = MathF.Max(1, MathF.Round(holder.Toxins / component.ToxinUptakeDivisor));
        if (holder.Toxins > component.ToxinsTolerance)
            holder.Health -= toxinUptake;

        holder.Toxins -= toxinUptake;

        if (holder.DrawWarnings)
            holder.UpdateSpriteAfterUpdate = true;
    }
}
