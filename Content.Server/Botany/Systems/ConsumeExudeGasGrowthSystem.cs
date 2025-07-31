using Content.Server.Atmos.EntitySystems;
using Content.Server.Botany.Components;
using Content.Shared.Atmos;

namespace Content.Server.Botany.Systems;
public sealed class ConsumeExudeGasGrowthSystem : PlantGrowthSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ConsumeExudeGasGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(EntityUid uid, ConsumeExudeGasGrowthComponent component, OnPlantGrowEvent args)
    {
        PlantHolderComponent? holder = null;
        Resolve<PlantHolderComponent>(uid, ref holder);

        if (holder == null || holder.Seed == null || holder.Dead)
            return;

        var environment = _atmosphere.GetContainingMixture(uid, true, true) ?? GasMixture.SpaceGas;

        holder.MissingGas = 0;
        if (component.ConsumeGasses.Count > 0)
        {
            foreach (var (gas, amount) in component.ConsumeGasses)
            {
                if (environment.GetMoles(gas) < amount)
                {
                    holder.MissingGas++;
                    continue;
                }

                environment.AdjustMoles(gas, -amount);
            }

            if (holder.MissingGas > 0)
            {
                holder.Health -= holder.MissingGas * HydroponicsSpeedMultiplier;
                if (holder.DrawWarnings)
                    holder.UpdateSpriteAfterUpdate = true;
            }
        }
    }
}
