using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Respiratory;
using Content.Server.Explosion;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Behaviors.Item;
using Content.Shared.Actions.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Audio;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
#pragma warning disable 618
    public class GasTankComponent : Component, IExamine, IGasMixtureHolder
#pragma warning restore 618
    {
        public override string Name => "GasTank";

        private const float MaxExplosionRange = 14f;
        private const float DefaultOutputPressure = Atmospherics.OneAtmosphere;

        private int _integrity = 3;

        [DataField("ruptureSound")] private SoundSpecifier _ruptureSound = new SoundPathSpecifier("Audio/Effects/spray.ogg");

        [DataField("air")] [ViewVariables] public GasMixture Air { get; set; } = new();

        /// <summary>
        ///     Distributed pressure.
        /// </summary>
        [DataField("outputPressure")]
        [ViewVariables]
        public float OutputPressure { get; private set; } = DefaultOutputPressure;

        /// <summary>
        ///     Pressure at which tanks start leaking.
        /// </summary>
        [DataField("tankLeakPressure")]
        public float TankLeakPressure { get; set; }     = 30 * Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Pressure at which tank spills all contents into atmosphere.
        /// </summary>
        [DataField("tankRupturePressure")]
        public float TankRupturePressure { get; set; }  = 40 * Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Base 3x3 explosion.
        /// </summary>
        [DataField("tankFragmentPressure")]
        public float TankFragmentPressure { get; set; } = 50 * Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Increases explosion for each scale kPa above threshold.
        /// </summary>
        [DataField("tankFragmentScale")]
        public float TankFragmentScale { get; set; }    = 10 * Atmospherics.OneAtmosphere;

        protected override void Initialize()
        {
            base.Initialize();
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("comp-gas-tank-examine", ("pressure", Math.Round(Air?.Pressure ?? 0))));
        }

        protected override void Shutdown()
        {
            base.Shutdown();
        }

        public GasMixture? RemoveAir(float amount)
        {
            var gas = Air?.Remove(amount);
            CheckStatus();
            return gas;
        }

        public GasMixture RemoveAirVolume(float volume)
        {
            if (Air == null)
                return new GasMixture(volume);

            var tankPressure = Air.Pressure;
            if (tankPressure < OutputPressure)
            {
                OutputPressure = tankPressure;
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new GasTankPressureDeficitEvent(tankPressure), false);
            }

            var molesNeeded = OutputPressure * volume / (Atmospherics.R * Air.Temperature);

            var air = RemoveAir(molesNeeded);

            if (air != null)
                air.Volume = volume;
            else
                return new GasMixture(volume);

            return air;
        }

        public void AssumeAir(GasMixture giver)
        {
            EntitySystem.Get<AtmosphereSystem>().Merge(Air, giver);
            CheckStatus();
        }

        public void CheckStatus()
        {
            if (Air == null)
                return;

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            var pressure = Air.Pressure;

            if (pressure > TankFragmentPressure)
            {
                // Give the gas a chance to build up more pressure.
                for (var i = 0; i < 3; i++)
                {
                    atmosphereSystem.React(Air, this);
                }

                pressure = Air.Pressure;
                var range = (pressure - TankFragmentPressure) / TankFragmentScale;

                // Let's cap the explosion, yeah?
                if (range > MaxExplosionRange)
                {
                    range = MaxExplosionRange;
                }

                Owner.SpawnExplosion((int) (range * 0.25f), (int) (range * 0.5f), (int) (range * 1.5f), 1);

                Owner.QueueDelete();
                return;
            }

            if (pressure > TankRupturePressure)
            {
                if (_integrity <= 0)
                {
                    var environment = atmosphereSystem.GetTileMixture(Owner.Transform.Coordinates, true);
                    if(environment != null)
                        atmosphereSystem.Merge(environment, Air);

                    SoundSystem.Play(Filter.Pvs(Owner), _ruptureSound.GetSound(), Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.125f));

                    Owner.QueueDelete();
                    return;
                }

                _integrity--;
                return;
            }

            if (pressure > TankLeakPressure)
            {
                if (_integrity <= 0)
                {
                    var environment = atmosphereSystem.GetTileMixture(Owner.Transform.Coordinates, true);
                    if (environment == null)
                        return;

                    var leakedGas = Air.RemoveRatio(0.25f);
                    atmosphereSystem.Merge(environment, leakedGas);
                }
                else
                {
                    _integrity--;
                }

                return;
            }

            if (_integrity < 3)
                _integrity++;
        }

        public class GasTankPressureDeficitEvent
        {
            float NewValue;

            public GasTankPressureDeficitEvent(float newValue)
            {
                NewValue = newValue;
            }
        }
    }
}
