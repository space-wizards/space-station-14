using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainers;
using Content.Shared.Temperature.Systems;

namespace Content.Client.Temperature.Systems;

/// <summary>
/// This exists so <see cref="SharedTemperatureSystem"/> runs on client/>
/// </summary>
public sealed class TemperatureSystem : SharedTemperatureSystem
{
    public override float ConductHeat(Entity<TemperatureComponent?> entity,
        ref HeatContainer heatContainer,
        float deltaT,
        float conductivityMod = 1,
        bool ignoreHeatResistance = false)
    {
        return 0f; // DO NOT RELY ON THIS VALUE TEMPERATURE EXCHANGE IS CURRENTLY NOT EVEN CLOSE TO BEING PREDICTED!!!
    }

    public override float ChangeHeat(Entity<TemperatureComponent?> entity, float heatAmount, bool ignoreHeatResistance = false)
    {
        return 0f; // DO NOT RELY ON THIS VALUE TEMPERATURE EXCHANGE IS CURRENTLY NOT EVEN CLOSE TO BEING PREDICTED!!!
    }
}
