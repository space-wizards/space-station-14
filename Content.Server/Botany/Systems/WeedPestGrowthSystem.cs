using Content.Server.Botany.Components;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;
public sealed class WeedPestGrowthSystem : PlantGrowthSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeedPestGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(EntityUid uid, WeedPestGrowthComponent component, OnPlantGrowEvent args)
    {
        PlantHolderComponent? holder = null;
        Resolve<PlantHolderComponent>(uid, ref holder);

        if (holder == null || holder.Seed == null || holder.Dead)
            return;

        // There's a small chance the pest population increases. Only happens with plants present.
        if (_random.Prob(0.01f))
        {
            holder.PestLevel += 0.5f * HydroponicsSpeedMultiplier;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }

        // Pest levels.
        if (holder.PestLevel > component.PestTolerance)
            holder.Health -= HydroponicsSpeedMultiplier;

        // Weed levels.
        if (holder.WeedLevel >= component.WeedTolerance)
            holder.Health -= HydroponicsSpeedMultiplier;
    }
}
