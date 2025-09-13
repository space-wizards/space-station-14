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

        // Consume Gasses
        foreach (var (gas, amount) in component.ConsumeGasses)
        {
            if (environment.GetMoles(gas) >= amount)
            {
                environment.AdjustMoles(gas, -amount);
            }
        }

        // Exude Gasses
        foreach (var (gas, amount) in component.ExudeGasses)
        {
            environment.AdjustMoles(gas, amount);
        }

        var containingMixture = _atmosphere.GetContainingMixture(uid, true, true);
        if (containingMixture != null)
        {
            _atmosphere.Merge(containingMixture, environment);
        }
    }
}
