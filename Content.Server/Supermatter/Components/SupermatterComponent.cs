using Content.Shared.Atmos;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using Robust.Shared.Analyzers;
using Content.Server.Supermatter.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization.Manager.Attributes;


namespace Content.Server.Supermatter.Components
{
    [RegisterComponent, Friend(typeof(SupermatterSystem))]
    public class SupermatterComponent : Component
    {
        public override string Name => "Supermatter";

        //TODO: clean all this up more
        //i've yet to see another component need so many variables and im not even using most of them
        //the gas constants like HeatPenalty are supermatter specific and shouldnt be used elsewhere

        [DataField("whitelist")]
        public EntityWhitelist Whitelist = new();

        [ViewVariables(VVAccess.ReadWrite)]
        public float Power {get; set;} = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public Atmos.GasMixture? Mix {get; set;}

        [ViewVariables(VVAccess.ReadOnly)]
        public float GasmixPowerRatio {get; set;} = 0;

        public float DynamicHeatModifier {get; set;} = 0;

        public float PowerTransmissionBonus {get; set;} = 0;

        public float PowerlossDynamicScaling {get; set;} = 0;

        public float DynamicHeatResistance {get; set;} = 0;

        public float PowerlossInhibitor {get; set;} = 0;

        //The damage we had before this cycle. Used to limit the damage we can take each cycle, and for safealert
        public float DamageArchived {get; set;} = 0;

        //Heat damage scales around this. Too hot setups with this amount of moles do regular damage, anything above and below is scaled
        public float  MoleHeatPenalty {get; set;} = 0f;

        //we yell if over 50 damage every YellTimer Seconds
        public const float YellTimer = 60f;

        //set to YellTimer at first so it doesnt yell a minute after being hit
        public float YellAccumulator {get; set;} = YellTimer;

        //TODO: PsyCoeff should change from 0-1 based on psycologist distance
        public float PsyCoeff = 0;

        //Are we exploding?
        private bool finalcountdown = false;

        //The point at which we should start sending messeges about the damage to the engi channels.
        public const float WarningPoint = 50;

        //The point at which we start sending messages to the common channel
        public const float EmergencyPoint = 700;

        //The point at which we delam
        public const int ExplosionPoint = 900;

        //delam alarm sound
        public SoundSpecifier DelamAlarm = new SoundPathSpecifier("/Audio/Machines/alarm.ogg");

        //When we pass this amount of damage we start shooting bolts
        private const int damagepenaltypoint = 550;

        //---------------------------------------------------------------------------------------\\

        public bool FinalCountdown {get; set;} = false;

        public float DamageUpdateAccumulator {get; set;}
        //update environment damage every second
        public const float DamageUpdateTimer = 1f;

        public float DelamTimerAccumulator {get; set;}
        public const int DelamTimerTimer = 30;
        public float SpeakAccumulator {get; set;} = 5f;
        public bool AlarmPlaying = false;

        public float AtmosUpdateAccumulator {get; set;}
        //update atmos every half second
        public const float AtmosUpdateTimer = 0.5f;

        //---------------------------------------------------------------------------------------\\

        //Higher == Higher percentage of inhibitor gas needed before the charge inertia chain reaction effect starts.
        public const float  PowerlossInhibitionGasThreshold = 0.20f;
        //Higher == More moles of the gas are needed before the charge inertia chain reaction effect starts.        //Scales powerloss inhibition down until this amount of moles is reached
        public const float  PowerlossInhibitionMoleThreshold = 20f;
        //bonus powerloss inhibition boost if this amount of moles is reached
        public const float  PowerlossInhibitionMoleBoostThreshold = 500f;

        //---------------------------------------------------------------------------------------\\

        //Above this value we can get lord singulo and independent mol damage, below it we can heal damage
        public const float  MolePenaltyThreshold = 1800f;

        //---------------------------------------------------------------------------------------\\

        //Along with damagepenaltypoint, makes flux anomalies.
        /// The cutoff for the minimum amount of power required to trigger the crystal invasion delamination event.
        public const float  EventPowerPenaltyThreshold = 4500f;
        //The cutoff on power properly doing damage, pulling shit around, and delamming into a tesla. Low chance of pyro anomalies, +2 bolts of electricity
        public const float  PowerPenaltyThreshold = 5000f;
        //+1 bolt of electricity, allows for gravitational anomalies, and higher chances of pyro anomalies
        public const float  SeverePowerPenaltyThreshold = 7000f;
        //+1 bolt of electricity.
        public const float  CriticalPowerPenaltyThreshold = 9000f;

        //---------------------------------------------------------------------------------------\\

        //Higher == Crystal safe operational temperature is higher.
        public const float  HeatPenaltyThreshold = 40f;
        //is multiplied by ExplosionPoint to cap evironmental damage per cycle
        public const float  DamageHardcap = 0.002f;
        //environmental damage is scaled by this
        public const float  DamageIncreaseMultiplier = 0.25f;
        //if spaced sm wont take more than 2 damage per cycle
        public const float MaxSpaceExposureDamage = 2;

        //---------------------------------------------------------------------------------------\\

        //Higher == more overall power
        public const float  ReactionPowerModefier = 0.55f;

        //---------------------------------------------------------------------------------------\\

        //These would be what you would get at point blank, decreases with distance
        public const float  DetonationRads = 200f;
        public const float  DetonationHallucination = 600f;

        //---------------------------------------------------------------------------------------\\

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
            //GasTrans,                             GasHeat,                              GasPowermix
            {Atmospherics.OxygenTransmitModifier,   Atmospherics.OxygenHeatPenalty,   Atmospherics.OxygenPowerMixRatio},   //oxygen
            {Atmospherics.NitrogenTransmitModifier, Atmospherics.NitrogenHeatPenalty, Atmospherics.NitrogenPowerMixRatio}, //nitrogen
            {Atmospherics.CO2TransmitModifier,      Atmospherics.CO2HeatPenalty,      Atmospherics.CO2PowerMixRatio},      //co2
            {Atmospherics.PlasmaTransmitModifier,   Atmospherics.PlasmaHeatPenalty,   Atmospherics.PlasmaPowerMixRatio},   //plasma
            {Atmospherics.TritiumTransmitModifier,  Atmospherics.TritiumHeatPenalty,  Atmospherics.TritiumPowerMixRatio},  //tritium
            {Atmospherics.WaterTransmitModifier,    Atmospherics.WaterHeatPenalty,    Atmospherics.WaterPowerMixRatio},    //water
        };
    }
}
