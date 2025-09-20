using System;
using Content.Server.Botany.Components;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Handles toxin tolerance and damage for plants.
/// </summary>
public sealed class ToxinsSystem : PlantGrowthSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToxinsComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(EntityUid uid, ToxinsComponent component, OnPlantGrowEvent args)
    {
        PlantHolderComponent? holder = null;
        Resolve<PlantHolderComponent>(uid, ref holder);

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
