using Content.Server.Body.Components;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.EntitySystems
{
    [UsedImplicitly]
    public class ThermalRegulatorSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var respirator in ComponentManager.EntityQuery<ThermalRegulatorComponent>(false))
            {
                var Owner = respirator.Owner;
                if (Owner.TryGetComponent<IMobStateComponent>(out var state) &&
                    state.IsDead())
                {
                    return;
                }

                respirator.AccumulatedFrametime += frameTime;

                if (respirator.AccumulatedFrametime < 1)
                {
                    return;
                }

                ProcessGases(respirator.AccumulatedFrametime);
                ProcessThermalRegulation(respirator.AccumulatedFrametime);

                respirator.AccumulatedFrametime -= 1;

                if (SuffocatingPercentage() > 0)
                {
                    TakeSuffocationDamage();
                    return;
                }

                StopSuffocation();
            }
        }

        private void ProcessThermalRegulation(float frameTime)
        {
            if (!Owner.TryGetComponent(out TemperatureComponent? temperatureComponent)) return;
            temperatureComponent.ReceiveHeat(MetabolismHeat);
            temperatureComponent.RemoveHeat(RadiatedHeat);

            // implicit heat regulation
            var tempDiff = Math.Abs(temperatureComponent.CurrentTemperature - NormalBodyTemperature);
            var targetHeat = tempDiff * temperatureComponent.HeatCapacity;
            if (temperatureComponent.CurrentTemperature > NormalBodyTemperature)
            {
                temperatureComponent.RemoveHeat(Math.Min(targetHeat, ImplicitHeatRegulation));
            }
            else
            {
                temperatureComponent.ReceiveHeat(Math.Min(targetHeat, ImplicitHeatRegulation));
            }

            // recalc difference and target heat
            tempDiff = Math.Abs(temperatureComponent.CurrentTemperature - NormalBodyTemperature);
            targetHeat = tempDiff * temperatureComponent.HeatCapacity;

            // if body temperature is not within comfortable, thermal regulation
            // processes starts
            if (tempDiff < ThermalRegulationTemperatureThreshold)
            {
                if (_isShivering || _isSweating)
                {
                    Owner.PopupMessage(Loc.GetString("metabolism-component-is-comfortable"));
                }

                _isShivering = false;
                _isSweating = false;
                return;
            }

            var actionBlocker = EntitySystem.Get<ActionBlockerSystem>();

            if (temperatureComponent.CurrentTemperature > NormalBodyTemperature)
            {
                if (!actionBlocker.CanSweat(Owner)) return;
                if (!_isSweating)
                {
                    Owner.PopupMessage(Loc.GetString("metabolism-component-is-sweating"));
                    _isSweating = true;
                }

                // creadth: sweating does not help in airless environment
                if (EntitySystem.Get<AtmosphereSystem>().GetTileMixture(Owner.Transform.Coordinates) is not {})
                {
                    temperatureComponent.RemoveHeat(Math.Min(targetHeat, SweatHeatRegulation));
                }
            }
            else
            {
                if (!actionBlocker.CanShiver(Owner)) return;
                if (!_isShivering)
                {
                    Owner.PopupMessage(Loc.GetString("metabolism-component-is-shivering"));
                    _isShivering = true;
                }

                temperatureComponent.ReceiveHeat(Math.Min(targetHeat, ShiveringHeatRegulation));
            }
        }
    }
}
