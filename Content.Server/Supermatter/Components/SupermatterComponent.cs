using System;
using System.Collections.Generic;
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
using Content.Shared.Radiation;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Analyzers;
using Content.Shared.Damage;


namespace Content.Server.Supermatter
{
    [RegisterComponent]
    public class SupermatterComponent : Component
    {
        public override string Name => "Supermatter";

        //TODO: clean all this up... or something
        //i've yet to see another component need so many variables and im not even using most of them
        //anything still using WHATEVER_THE_FUCK_THIS_IS is not used at the moment and should be considered for deletion
        private float _Power = 0;
        private Atmos.GasMixture? _mix;
        private float _PowerRatio = 0;
        private float _DynamicHeatModifier = 0;
        private float _PowerTransmissionBonus = 0;
        private float _PowerlossDynamicScaling = 0;
        private float _DynamicHeatResistance = 0;
        private float _PowerlossInhibitor = 0;
        private float _damageArchived = 0;
        //Heat damage scales around this. Too hot setups with this amount of moles do regular damage, anything above and below is scaled
        private float  _moleHeatPenalty = 0f;

        private float _yellaccumulator = 60f;
        private const float _yellTimer = 60f;

        //TODO: PsyCoeff should change from 0-1 based on psycologist distance
        public float PsyCoeff = 0;

        //Are we exploding?
        private bool final_countdown = false;

        //The damage we had before this cycle. Used to limit the damage we can take each cycle, and for safe_alert
        private int damage_archived = 0;
        //Our "Shit is no longer fucked" message. We send it when damage is less then damage_archived
        public const string SafeAlert = "Crystalline hyperstructure returning to safe operating parameters.";
        //The point at which we should start sending messeges about the damage to the engi channels.
        public float WarningPoint = 50;
        ///The alert we send when we've reached warning_point
        private const string warning_alert = "Danger! Crystal hyperstructure integrity faltering!";
        //The point at which we start sending messages to the common channel
        public float EmergencyPoint = 700;
        //The alert we send when we've reached emergency_point
        private const string emergency_alert = "CRYSTAL DELAMINATION IMMINENT.";
        //The point at which we delam
        public readonly int ExplosionPoint = 900;
        //delam alarm sound
        public SoundSpecifier DelamAlarm = new SoundPathSpecifier("/Audio/Machines/alarm.ogg");
        //When we pass this amount of damage we start shooting bolts
        private const int damage_penalty_point = 550;
        public bool _finalCountdown = false;

        //Higher == Bigger heat and waste penalty from having the crystal surrounded by this gas. Negative numbers reduce penalty.
        //public static float PLASMA_HEAT_PENALTY = 15f;
        //public static float OXYGEN_HEAT_PENALTY = 1f;
        //Better then co2, worse then n2
        public static float PLUOXIUM_HEAT_PENALTY = -0.5f;
        public static float TRITIUM_HEAT_PENALTY = 10f;
        public static float CO2_HEAT_PENALTY = 0.1f;
        //public static float NITROGEN_HEAT_PENALTY = -1.5f;
        public static float BZ_HEAT_PENALTY = 5f;
        //This'll get made slowly over time, I want my spice rock spicy god damnit
        //public static float H2O_HEAT_PENALTY = 12f;
        //very good heat absorbtion and less plasma and o2 generation
        public static float FREON_HEAT_PENALTY = -10f;
        // similar heat penalty as tritium (dangerous)
        public static float HYDROGEN_HEAT_PENALTY = 10f;
        public static float HEALIUM_HEAT_PENALTY = 4f;
        public static float PROTO_NITRATE_HEAT_PENALTY = -3f;
        public static float ZAUKER_HEAT_PENALTY = 8f;

        //All of these get divided by 10-bzcomp * 5 before having 1 added and being multiplied with power to determine rads
        //Keep the negative values here above -10 and we won't get negative rads
        //Higher == Bigger bonus to power generation.
        //public static float OXYGEN_TRANSMIT_MODIFIER = 1.5f;
        //public static float PLASMA_TRANSMIT_MODIFIER = 4f;
        public static float BZ_TRANSMIT_MODIFIER = -2f;
        //We divide by 10, so this works out to 3
        //public static float TRITIUM_TRANSMIT_MODIFIER = 30f;
        //Should halve the power output
        public static float PLUOXIUM_TRANSMIT_MODIFIER = -5f;
        //public static float H2O_TRANSMIT_MODIFIER = 2f;
        //increase the radiation emission, but less than the trit (2.5)
        public static float HYDROGEN_TRANSMIT_MODIFIER = 25f;
        public static float HEALIUM_TRANSMIT_MODIFIER = 2.4f;
        public static float PROTO_NITRATE_TRANSMIT_MODIFIER = 15f;
        public static float ZAUKER_TRANSMIT_MODIFIER = 20f;

        //Improves the effect of transmit modifiers
        public static float BZ_RADIOACTIVITY_MODIFIER = 5f;

        //Higher == Gas makes the crystal more resistant against heat damage.
        public static float N2O_HEAT_RESISTANCE = 6f;
        // just a bit of heat resistance to spice it up
        public static float HYDROGEN_HEAT_RESISTANCE = 2f;
        public static float PROTO_NITRATE_HEAT_RESISTANCE = 5f;

        // The minimum portion of the miasma in the air that will be consumed. Higher values mean more miasma will be consumed be default.
        public static float MIASMA_CONSUMPTION_RATIO_MIN = 0f;
        // The maximum portion of the miasma in the air that will be consumed. Lower values mean the miasma consumption rate caps earlier.
        public static float MIASMA_CONSUMPTION_RATIO_MAX = 1f;
        // The minimum pressure for a pure miasma atmosphere to begin being consumed. Higher values mean it takes more miasma pressure to make miasma start being consumed. Should be >= 0
        public static float MIASMA_CONSUMPTION_PP = (Atmospherics.OneAtmosphere*0.01f);
        // How the amount of miasma consumed per tick scales with partial pressure. Higher values decrease the rate miasma consumption scales with partial pressure. Should be >0
        public static float MIASMA_PRESSURE_SCALING = (Atmospherics.OneAtmosphere*0.5f);
        // How much the amount of miasma consumed per tick scales with gasmix power ratio. Higher values means gasmix has a greater effect on the miasma consumed.
        public static float MIASMA_GASMIX_SCALING = (0.3f);
        // The amount of matter power generated for every mole of miasma consumed. Higher values mean miasma generates more power.
        public static float MIASMA_POWER_GAIN = 10f;

        //Higher == Higher percentage of inhibitor gas needed before the charge inertia chain reaction effect starts.
        public static float  PowerlossInhibitionGasThreshold = 0.20f;
        //Higher == More moles of the gas are needed before the charge inertia chain reaction effect starts.        //Scales powerloss inhibition down until this amount of moles is reached
        public static float  PowerlossInhibitionMoleThreshold = 20f;
        //bonus powerloss inhibition boost if this amount of moles is reached
        public static float  PowerlossInhibitionMoleBoostThreshold = 500f;
        //Above this value we can get lord singulo and independent mol damage, below it we can heal damage
        public static float  MolePenaltyThreshold = 1800f;

        //Along with damage_penalty_point, makes flux anomalies.
        /// The cutoff for the minimum amount of power required to trigger the crystal invasion delamination event.
        public static float  EVENT_POWER_PENALTY_THRESHOLD = 4500f;
        //The cutoff on power properly doing damage, pulling shit around, and delamming into a tesla. Low chance of pyro anomalies, +2 bolts of electricity
        public static float  PowerPenaltyThreshold = 5000f;
        //+1 bolt of electricity, allows for gravitational anomalies, and higher chances of pyro anomalies
        public static float  SEVERE_POWER_PENALTY_THRESHOLD = 7000f;
        //+1 bolt of electricity.
        public static float  CRITICAL_POWER_PENALTY_THRESHOLD = 9000f;
        //Higher == Crystal safe operational temperature is higher.
        public static float  HeatPenaltyThreshold = 40f;
        public readonly float  DamageHardcap = 0.002f;
        public readonly float  DamageIncreaseMultiplier = 0.25f;

        //Higher == less heat released during reaction, not to be confused with the above values
        public static float  ThermalReleaseModifier = 5f;
        //Higher == less plasma released by reaction
        public static float  PlasmaReleaseModifier = 750f;
        //Higher == less oxygen released at high temperature/power
        public static float  OxygenReleaseModifier = 325f;
        //Higher == more overall power
        public static float  ReactionPowerModefier = 0.55f;
        //Crystal converts 1/this value of stored matter into energy.
        public static float  MATTER_POWER_CONVERSION = 10f;
        //These would be what you would get at point blank, decreases with distance
        public static float  DETONATION_RADS = 200f;
        public static float  DETONATION_HALLUCINATION = 600f;

        public static float MaxSpaceExposureDamage = 2;

        [ViewVariables(VVAccess.ReadOnly)]
        public float[] GasComp =
        {
            0, //oxy
            0, //Nit
            0, //Co2
            0, //pla
            0, //tri
            0, //h2o
        };

        public readonly float[,] gasFacts =
        {
            //GasTrans, GasHeat, GasPowermix
            {1.5f, 1f, 1f},   //oxy
            {0f, -1.5f, -1f}, //nit
            {0f, 0f, 1f},     //co2
            {4f, 15f, 1f},    //pla
            {30f, 0f, 1f},    //tri
            {2f, 12f, 1f}     //h2o
        };

        [ViewVariables(VVAccess.ReadWrite)]
        public float Power
        {
            get => _Power;
            set
            {
                _Power = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadOnly)]
        public float GasmixPowerRatio
        {
            get => _PowerRatio;
            set
            {
                _PowerRatio = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Atmos.GasMixture? Mix
        {
            get => _mix;
            set
            {
                _mix = value;
                Dirty();
            }
        }

        public float DynamicHeatModifier
        {
            get => _DynamicHeatModifier;
            set
            {
                _DynamicHeatModifier = value;
                Dirty();
            }
        }

        public float DynamicHeatResistance
        {
            get => _DynamicHeatResistance;
            set
            {
                _DynamicHeatResistance = value;
                Dirty();
            }
        }

        public float PowerTransmissionBonus
        {
            get => _PowerTransmissionBonus;
            set
            {
                _PowerTransmissionBonus = value;
                Dirty();
            }
        }

        public float PowerlossDynamicScaling
        {
            get => _PowerlossDynamicScaling;
            set
            {
                _PowerlossDynamicScaling = value;
                Dirty();
            }
        }

        public float PowerlossInhibitor
        {
            get => _PowerlossInhibitor;
            set
            {
                _PowerlossInhibitor = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool FinalCountdown
        {
            get => _finalCountdown;
            set
            {
                _finalCountdown = value;
                Dirty();
            }
        }

        public float DamageArchived
        {
            get => _damageArchived;
            set
            {
                _damageArchived = value;
                Dirty();
            }
        }

        public float MoleHeatPenalty
        {
            get => _moleHeatPenalty;
            set
            {
                _moleHeatPenalty = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float YellAccumulator
        {
            get => _yellaccumulator;

            set
            {
                _yellaccumulator = value;
                Dirty();
            }
        }
    }
}
