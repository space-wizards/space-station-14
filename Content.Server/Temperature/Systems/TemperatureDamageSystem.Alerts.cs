using Content.Server.Body.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Server.Temperature.Systems;

public sealed partial class TemperatureDamageSystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    /// <summary>
    /// 
    /// </summary>
    private void ServerAlert(EntityUid uid, AlertsComponent status, OnTemperatureChangeEvent args)
    {
        ProtoId<AlertPrototype> type;
        float threshold;
        float idealTemp;

        if (!TryComp<TemperatureComponent>(uid, out var temperature))
        {
            _alerts.ClearAlertCategory(uid, TemperatureAlertCategory);
            return;
        }

        if (TryComp<ThermalRegulatorComponent>(uid, out var regulator) &&
            regulator.NormalBodyTemperature > temperature.ColdDamageThreshold &&
            regulator.NormalBodyTemperature < temperature.HeatDamageThreshold)
        {
            idealTemp = regulator.NormalBodyTemperature;
        }
        else
        {
            idealTemp = (temperature.ColdDamageThreshold + temperature.HeatDamageThreshold) / 2;
        }

        if (args.CurrentTemperature <= idealTemp)
        {
            type = temperature.ColdAlert;
            threshold = temperature.ColdDamageThreshold;
        }
        else
        {
            type = temperature.HotAlert;
            threshold = temperature.HeatDamageThreshold;
        }

        // Calculates a scale where 1.0 is the ideal temperature and 0.0 is where temperature damage begins
        // The cold and hot scales will differ in their range if the ideal temperature is not exactly halfway between the thresholds
        var tempScale = (args.CurrentTemperature - threshold) / (idealTemp - threshold);
        switch (tempScale)
        {
            case <= 0f:
                _alerts.ShowAlert(uid, type, 3);
                break;

            case <= 0.4f:
                _alerts.ShowAlert(uid, type, 2);
                break;

            case <= 0.66f:
                _alerts.ShowAlert(uid, type, 1);
                break;

            case > 0.66f:
                _alerts.ClearAlertCategory(uid, TemperatureAlertCategory);
                break;
        }
    }

    /// <summary>
    /// Handles updating the "too hot!" and "too cold!" alerts in response to changes in temperature.
    /// </summary>
    /// <param name="entity">The entity that host the alerts.</param>
    /// <param name="args">A change of the temperature of the given entity.</param>
    private void OnTemperatureChange(Entity<AlertsComponent> entity, ref OnTemperatureChangeEvent args)
    {
        AlertType type;
        float threshold;
        float idealTemp;

        if (!TryComp<TemperatureDamageThresholdsComponent>(entity, out var thresholds))
        {
            _alertsSystem.ClearAlertCategory(entity, AlertCategory.Temperature);
            return;
        }

        if (TryComp<ThermalRegulatorComponent>(entity, out var regulator) &&
            regulator.NormalBodyTemperature > thresholds.ColdDamageThreshold &&
            regulator.NormalBodyTemperature < thresholds.HeatDamageThreshold)
        {
            idealTemp = regulator.NormalBodyTemperature;
        }
        else
        {
            idealTemp = (thresholds.ColdDamageThreshold + thresholds.HeatDamageThreshold) / 2;
        }

        if (args.CurrentTemperature <= idealTemp)
        {
            type = AlertType.Cold;
            threshold = thresholds.ColdDamageThreshold;
        }
        else
        {
            type = AlertType.Hot;
            threshold = thresholds.HeatDamageThreshold;
        }

        // Calculates a scale where 1.0 is the ideal temperature and 0.0 is where temperature damage begins
        // The cold and hot scales will differ in their range if the ideal temperature is not exactly halfway between the thresholds
        var tempScale = (args.CurrentTemperature - threshold) / (idealTemp - threshold);
        switch (tempScale)
        {
            case <= 0f:
                _alertsSystem.ShowAlert(entity, type, 3);
                break;

            case <= 0.4f:
                _alertsSystem.ShowAlert(entity, type, 2);
                break;

            case <= 0.66f:
                _alertsSystem.ShowAlert(entity, type, 1);
                break;

            case > 0.66f:
                _alertsSystem.ClearAlertCategory(entity, AlertCategory.Temperature);
                break;
        }
    }
}
