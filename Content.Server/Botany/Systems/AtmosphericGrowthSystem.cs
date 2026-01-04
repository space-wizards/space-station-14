using Content.Server.Atmos.EntitySystems;
using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
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
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AtmosphericGrowthComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
        SubscribeLocalEvent<AtmosphericGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnCrossPollinate(Entity<AtmosphericGrowthComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<AtmosphericGrowthComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossFloat(ref ent.Comp.IdealHeat, pollenData.IdealHeat);
        _mutation.CrossFloat(ref ent.Comp.HeatTolerance, pollenData.HeatTolerance);
        _mutation.CrossFloat(ref ent.Comp.LowPressureTolerance, pollenData.LowPressureTolerance);
        _mutation.CrossFloat(ref ent.Comp.HighPressureTolerance, pollenData.HighPressureTolerance);
    }

    private void OnPlantGrow(Entity<AtmosphericGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        if (!TryComp<PlantHolderComponent>(ent.Owner, out var holder))
            return;

        var environment = _atmosphere.GetContainingMixture(ent.Owner, true, true) ?? GasMixture.SpaceGas;
        if (MathF.Abs(environment.Temperature - ent.Comp.IdealHeat) > ent.Comp.HeatTolerance)
        {
            _plantHolder.AdjustsHealth(ent.Owner, -_random.Next(1, 3));
            holder.ImproperHeat = true;
        }
        else
        {
            holder.ImproperHeat = false;
        }

        var pressure = environment.Pressure;
        if (pressure < ent.Comp.LowPressureTolerance || pressure > ent.Comp.HighPressureTolerance)
        {
            _plantHolder.AdjustsHealth(ent.Owner, -_random.Next(1, 3));
            holder.ImproperPressure = true;
        }
        else
        {
            holder.ImproperPressure = false;
        }
    }
}
