using Content.Server.Body.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Alert;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Temperature.Systems;

public sealed partial class TemperatureDamageSystem
{
    private void OnTemperatureChanged(Entity<AlertsComponent> entity, ref OnTemperatureChangeEvent args)
    {
        ProtoId<AlertPrototype> type;
        float threshold;
        float idealTemp;

        if (!TryComp<TemperatureDamageThresholdsComponent>(entity, out var thresholds))
        {
            _alertsSystem.ClearAlertCategory(entity, TemperatureAlertCategory);
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
            type = thresholds.ColdAlert;
            threshold = thresholds.ColdDamageThreshold;
        }
        else
        {
            type = thresholds.HotAlert;
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
                _alertsSystem.ClearAlertCategory(entity, TemperatureAlertCategory);
                break;
        }
    }
}
