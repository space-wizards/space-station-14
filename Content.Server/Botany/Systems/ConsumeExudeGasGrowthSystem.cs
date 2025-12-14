using Content.Server.Atmos.EntitySystems;
using Content.Server.Botany.Components;
using Content.Shared.Atmos;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Consumes and emits configured gases around plants each growth tick, then merges
/// the adjusted gas mixture back into the environment.
/// </summary>
public sealed class ConsumeExudeGasGrowthSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ConsumeExudeGasGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<ConsumeExudeGasGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        if (!TryComp(uid, out PlantHolderComponent? holder)
            || !TryComp(uid, out PlantComponent? plant))
            return;

        var environment = _atmosphere.GetContainingMixture(uid, true, true) ?? GasMixture.SpaceGas;

        // Consume Gasses.
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
                holder.Health -= holder.MissingGas * BasicGrowthSystem.HydroponicsSpeedMultiplier;
                if (holder.DrawWarnings)
                    holder.UpdateSpriteAfterUpdate = true;
            }
        }

        // Exude Gasses.
        var exudeCount = component.ExudeGasses.Count;
        if (exudeCount > 0)
        {
            foreach (var (gas, amount) in component.ExudeGasses)
            {
                environment.AdjustMoles(gas,
                    MathF.Max(1f, MathF.Round(amount * MathF.Round(plant.Potency) / exudeCount)));
            }
        }
    }
}
