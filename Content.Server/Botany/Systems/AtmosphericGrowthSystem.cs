using Content.Server.Atmos.EntitySystems;
using Content.Server.Botany.Components;
using Content.Shared.Atmos;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Applies atmospheric temperature and pressure effects to plants during growth ticks.
/// Uses current tile gas mixture to penalize or clear warnings based on tolerances.
/// </summary>
public sealed class AtmosphericGrowthSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosphericGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<AtmosphericGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        PlantHolderComponent? holder = null;
        if (!Resolve(uid, ref holder))
            return;

        var environment = _atmosphere.GetContainingMixture(uid, true, true) ?? GasMixture.SpaceGas;
        if (MathF.Abs(environment.Temperature - component.IdealHeat) > component.HeatTolerance)
        {
            holder.Health -= _random.Next(1, 3);
            holder.ImproperHeat = true;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }
        else
        {
            holder.ImproperHeat = false;
        }

        var pressure = environment.Pressure;
        if (pressure < component.LowPressureTolerance || pressure > component.HighPressureTolerance)
        {
            holder.Health -= _random.Next(1, 3);
            holder.ImproperPressure = true;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }
        else
        {
            holder.ImproperPressure = false;
        }
    }
}
