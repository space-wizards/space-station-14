using Content.Server.Temperature.Components;
using Content.Shared.Medical.Disease;
using Content.Server.Temperature.Systems;

namespace Content.Server.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomTemperature : SymptomBehavior
{
    /// <summary>
    /// Target body temperature (K) to move towards.
    /// </summary>
    [DataField]
    public float TargetTemperature { get; private set; } = 310.15f; // 37.0 °C

    /// <summary>
    /// Maximum delta (K) applied per trigger.
    /// </summary>
    [DataField]
    public float StepTemperature { get; private set; } = 60f;
}

public sealed partial class SymptomTemperature
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly TemperatureSystem _temperatureSystem = default!;

    /// <summary>
    /// Adjusts the carrier's body temperature towards a target in small heat steps.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseasePrototype disease)
    {
        if (!_entityManager.TryGetComponent(uid, out TemperatureComponent? temperature))
            return;

        var target = TargetTemperature;
        var current = temperature.CurrentTemperature;
        if (Math.Abs(current - target) < 0.01f)
            return;

        var degrees = Math.Sign(target - current) * Math.Min(Math.Abs(target - current), StepTemperature);
        var heatCap = _temperatureSystem.GetHeatCapacity(uid);
        var heat = degrees * heatCap;
        _temperatureSystem.ChangeHeat(uid, heat, ignoreHeatResistance: true, temperature);
    }
}
