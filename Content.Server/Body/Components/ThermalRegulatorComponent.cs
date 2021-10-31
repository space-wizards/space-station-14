using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    /// <summary>
    ///     Handles body temperature
    /// </summary>
    [RegisterComponent]
    public class ThermalRegulatorComponent : Component
    {
        public override string Name => "ThermalRegulator";

        public float AccumulatedFrametime;

        public bool IsShivering;
        public bool IsSweating;

        /// <summary>
        /// Heat generated due to metabolism. It's generated via metabolism
        /// </summary>
        [ViewVariables]
        [DataField("metabolismHeat")]
        public float MetabolismHeat { get; private set; }

        /// <summary>
        /// Heat output via radiation.
        /// </summary>
        [ViewVariables]
        [DataField("radiatedHeat")]
        public float RadiatedHeat { get; private set; }

        /// <summary>
        /// Maximum heat regulated via sweat
        /// </summary>
        [ViewVariables]
        [DataField("sweatHeatRegulation")]
        public float SweatHeatRegulation { get; private set; }

        /// <summary>
        /// Maximum heat regulated via shivering
        /// </summary>
        [ViewVariables]
        [DataField("shiveringHeatRegulation")]
        public float ShiveringHeatRegulation { get; private set; }

        /// <summary>
        /// Amount of heat regulation that represents thermal regulation processes not
        /// explicitly coded.
        /// </summary>
        [DataField("implicitHeatRegulation")]
        public float ImplicitHeatRegulation { get; private set; }

        /// <summary>
        /// Normal body temperature
        /// </summary>
        [ViewVariables]
        [DataField("normalBodyTemperature")]
        public float NormalBodyTemperature { get; private set; }

        /// <summary>
        /// Deviation from normal temperature for body to start thermal regulation
        /// </summary>
        [DataField("thermalRegulationTemperatureThreshold")]
        public float ThermalRegulationTemperatureThreshold { get; private set; }

        public void ProcessThermalRegulation(float frameTime)
        {
            if (!Owner.TryGetComponent(out TemperatureComponent? temp)) return;
            var tempsys = EntitySystem.Get<TemperatureSystem>();
            tempsys.ReceiveHeat(Owner.Uid, MetabolismHeat, temp);
            tempsys.RemoveHeat(Owner.Uid, RadiatedHeat, temp);

            // implicit heat regulation
            var tempDiff = Math.Abs(temp.CurrentTemperature - NormalBodyTemperature);
            var targetHeat = tempDiff * temp.HeatCapacity;
            if (temp.CurrentTemperature > NormalBodyTemperature)
            {
                tempsys.RemoveHeat(Owner.Uid, Math.Min(targetHeat, ImplicitHeatRegulation), temp);
            }
            else
            {
                tempsys.ReceiveHeat(Owner.Uid, Math.Min(targetHeat, ImplicitHeatRegulation), temp);
            }

            // recalc difference and target heat
            tempDiff = Math.Abs(temp.CurrentTemperature - NormalBodyTemperature);
            targetHeat = tempDiff * temp.HeatCapacity;

            // if body temperature is not within comfortable, thermal regulation
            // processes starts
            if (tempDiff < ThermalRegulationTemperatureThreshold)
            {
                if (IsShivering || IsSweating)
                {
                    Owner.PopupMessage(Loc.GetString("metabolism-component-is-comfortable"));
                }

                IsShivering = false;
                IsSweating = false;
                return;
            }

            var actionBlocker = EntitySystem.Get<ActionBlockerSystem>();

            if (temp.CurrentTemperature > NormalBodyTemperature)
            {
                if (!actionBlocker.CanSweat(Owner)) return;
                if (!IsSweating)
                {
                    Owner.PopupMessage(Loc.GetString("metabolism-component-is-sweating"));
                    IsSweating = true;
                }

                // creadth: sweating does not help in airless environment
                if (EntitySystem.Get<AtmosphereSystem>().GetTileMixture(Owner.Transform.Coordinates) is not {})
                {
                    tempsys.RemoveHeat(Owner.Uid, Math.Min(targetHeat, SweatHeatRegulation), temp);
                }
            }
            else
            {
                if (!actionBlocker.CanShiver(Owner)) return;
                if (!IsShivering)
                {
                    Owner.PopupMessage(Loc.GetString("metabolism-component-is-shivering"));
                    IsShivering = true;
                }

                tempsys.ReceiveHeat(Owner.Uid, Math.Min(targetHeat, ShiveringHeatRegulation), temp);
            }
        }
    }
}
