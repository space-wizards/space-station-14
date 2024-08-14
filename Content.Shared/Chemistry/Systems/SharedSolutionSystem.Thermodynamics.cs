using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{
    public const float TemperatureEpsilon = 0.0005f;


    protected void ChangeTotalVolume(Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData origin,
        FixedPoint2 delta,
        float? temperature)
    {
        SetTotalVolume(solution, solution.Comp.Volume + delta);
        ChangeHeatCapacity(solution, CalcHeatCapacity(origin.ReagentEnt.Comp.SpecificHeat, delta), false);
        if (temperature != null)
            ChangeThermalEnergy(solution, CalcThermalEnergy(origin.ReagentEnt.Comp.SpecificHeat, delta, temperature.Value));
        UpdatePrimaryReagent(solution, ref origin, delta);
    }

    protected void SetTotalVolume(Entity<SolutionComponent> solution, FixedPoint2 newVolume)
    {
        newVolume = FixedPoint2.Max(0, newVolume);
        if (newVolume == solution.Comp.Volume)
            return;
        solution.Comp.Volume = newVolume;
    }

    protected void ChangeHeatCapacity(Entity<SolutionComponent> solution, float heatCapacity, bool changesTemp)
    {
        if (heatCapacity == 0)
            return;
        solution.Comp.HeatCapacity += heatCapacity;
        if (changesTemp)
        {
            UpdateTemperature(solution);
            return;
        }
        solution.Comp.ThermalEnergy = CalcThermalEnergy(heatCapacity, solution.Comp.Temperature);
    }

    public void ChangeThermalEnergy(Entity<SolutionComponent> solution, float thermalDelta)
    {
        if (thermalDelta == 0)
            return;
        solution.Comp.ThermalEnergy += thermalDelta;
        UpdateTemperature(solution);
    }

    public void ChangeTemperature(Entity<SolutionComponent> solution, float tempDelta)
    {
        if (tempDelta == 0)
            return;
        solution.Comp.ThermalEnergy += tempDelta*solution.Comp.HeatCapacity;
        UpdateTemperature(solution);
    }

    public void SetThermalEnergy(Entity<SolutionComponent> solution, float newThermalEnergy)
    {
        if (MathF.Abs(newThermalEnergy - solution.Comp.ThermalEnergy) < TemperatureEpsilon)
            return;
        solution.Comp.ThermalEnergy = newThermalEnergy;
        solution.Comp.Temperature = CalcTemperature(solution.Comp.HeatCapacity, newThermalEnergy);
    }

    public void SetTemperature(Entity<SolutionComponent> solution, float newTemperature)
    {
        SetThermalEnergy(solution,CalcThermalEnergy(solution.Comp.HeatCapacity, newTemperature));
    }

    private void UpdateTemperature(Entity<SolutionComponent> solution)
    {
        solution.Comp.Temperature = solution.Comp.ThermalEnergy / solution.Comp.HeatCapacity;
    }

    public void ClearThermalEnergy(Entity<SolutionComponent> solution, bool resetTemp = true)
    {
        solution.Comp.ThermalEnergy = 0;
        if (resetTemp)
            solution.Comp.Temperature = Atmospherics.T20C;
    }

    public void ClearHeatCapacity(Entity<SolutionComponent> solution, bool resetTemp = false)
    {
        solution.Comp.HeatCapacity = 0;
        if (resetTemp)
            solution.Comp.Temperature = Atmospherics.T20C;
    }

    public void RecalculateThermalEnergy(Entity<SolutionComponent> solution)
    {
        if (solution.Comp.HeatCapacity == 0)
        {
            if (solution.Comp.Volume == 0)
            {
                solution.Comp.ThermalEnergy = 0;
                return;
            }
            RecalculateHeatCapacity(solution);
        }
        solution.Comp.ThermalEnergy = CalcThermalEnergy(solution.Comp.HeatCapacity, solution.Comp.Temperature);
        UpdateTemperature(solution);
    }

    public void RecalculateHeatCapacity(Entity<SolutionComponent> solution)
    {
        solution.Comp.HeatCapacity = 0;
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            solution.Comp.HeatCapacity += CalcHeatCapacity(reagentData.ReagentEnt.Comp.SpecificHeat, reagentData.TotalQuantity);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CalcHeatCapacity(float specificHeat, FixedPoint2 quantity) =>
        specificHeat * quantity.Float();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CalcThermalEnergy(float heatCapacity, float temperature) => heatCapacity * temperature;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public float CalcTemperature(float heatCapacity, float thermalEnergy) => thermalEnergy/heatCapacity;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CalcThermalEnergy(float specificHeat, FixedPoint2 quantity, float temperature) =>
        CalcThermalEnergy(quantity.Float() * specificHeat, temperature);
}
