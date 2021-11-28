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

        //Our "Shit is no longer fucked" message. We send it when damage is less then damagearchived
        public const string SafeAlert = "Crystalline hyperstructure returning to safe operating parameters.";

        //The point at which we should start sending messeges about the damage to the engi channels.
        public const float WarningPoint = 50;

        ///The alert we send when we've reached warningpoint
        private const string warningalert = "Danger! Crystal hyperstructure integrity faltering!";

        //The point at which we start sending messages to the common channel
        public const float EmergencyPoint = 700;

        //The alert we send when we've reached emergencypoint
        private const string emergencyalert = "CRYSTAL DELAMINATION IMMINENT.";

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

        //---------------------------------------------------------------------------------------\\

        //TODO: move to gasFacts once/if these are in
        //Higher == Bigger heat and waste penalty from having the crystal surrounded by this gas. Negative numbers reduce penalty.
        //public const float PlasmaHeatPenalty = 15f;
        //public const float OXYGENHeatPenalty = 1f;
        //Better then co2, worse then n2
        public const float PluoxiumHeatPenalty = -0.5f;
        public const float TritiumHeatPenalty = 10f;
        public const float CO2HeatPenalty = 0.1f;
        //public const float NITROGENHeatPenalty = -1.5f;
        public const float BZHeatPenalty = 5f;
        //This'll get made slowly over time, I want my spice rock spicy god damnit
        //public const float H2OHeatPenalty = 12f;
        //very good heat absorbtion and less plasma and o2 generation
        public const float FreonHeatPenalty = -10f;
        // similar heat penalty as tritium (dangerous)
        public const float HydrogenHeatPenalty = 10f;
        public const float HealiumHeatPenalty = 4f;
        public const float ProtonitrateHeatPenalty = -3f;
        public const float ZaukerHeatPenalty = 8f;

        //---------------------------------------------------------------------------------------\\

        //TODO: move to gasFacts once/if these are in
        //All of these get divided by 10-bzcomp * 5 before having 1 added and being multiplied with power to determine rads
        //Keep the negative values here above -10 and we won't get negative rads
        //Higher == Bigger bonus to power generation.
        //public const float OXYGENTransmitModifier = 1.5f;
        //public const float PlasmaTransmitModifier = 4f;
        public const float BZTransmitModifier = -2f;
        //We divide by 10, so this works out to 3
        //public const float TritiumTransmitModifier = 30f;
        //Should halve the power output
        public const float PluoxiumTransmitModifier = -5f;
        //public const float H2OTransmitModifier = 2f;
        //increase the radiation emission, but less than the trit (2.5)
        public const float HydrogenTransmitModifier = 25f;
        public const float HealiumTransmitModifier = 2.4f;
        public const float ProtonitrateTransmitModifier = 15f;
        public const float ZaukerTransmitModifier = 20f;

        //---------------------------------------------------------------------------------------\\

        //Improves the effect of transmit modifiers
        public const float BZRadioactivityModifier = 5f;

        //---------------------------------------------------------------------------------------\\

        //TODO: move to gasFacts once/if these are in
        //Higher == Gas makes the crystal more resistant against heat damage.
        public const float N2OHeatResistance = 6f;
        // just a bit of heat resistance to spice it up
        public const float HydrogenHeatResistance = 2f;
        public const float ProtoNitrateHeatResistance = 5f;

        //---------------------------------------------------------------------------------------\\

        //TODO: move to gasFacts if miasma is ported
        // The minimum portion of the miasma in the air that will be consumed. Higher values mean more miasma will be consumed be default.
        public const float MiasmaConsumptionRatioMin = 0f;
        // The maximum portion of the miasma in the air that will be consumed. Lower values mean the miasma consumption rate caps earlier.
        public const float MiasmaConsumptionRatioMax = 1f;
        // The minimum pressure for a pure miasma atmosphere to begin being consumed. Higher values mean it takes more miasma pressure to make miasma start being consumed. Should be >= 0
        public const float MiasmaConsumptionPP = (Atmospherics.OneAtmosphere*0.01f);
        // How the amount of miasma consumed per tick scales with partial pressure. Higher values decrease the rate miasma consumption scales with partial pressure. Should be >0
        public const float MiasmaPressureScaling = (Atmospherics.OneAtmosphere*0.5f);
        // How much the amount of miasma consumed per tick scales with gasmix power ratio. Higher values means gasmix has a greater effect on the miasma consumed.
        public const float MiasmaGasMixScaling = (0.3f);
        // The amount of matter power generated for every mole of miasma consumed. Higher values mean miasma generates more power.
        public const float MiasmaPowerGain = 10f;

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

        //Higher == less heat released during reaction, not to be confused with the above values
        public const float  ThermalReleaseModifier = 5f;
        //Higher == less plasma released by reaction
        public const float  PlasmaReleaseModifier = 750f;
        //Higher == less oxygen released at high temperature/power
        public const float  OxygenReleaseModifier = 325f;
        //Higher == more overall power
        public const float  ReactionPowerModefier = 0.55f;
        //Crystal converts 1/this value of stored matter into energy.
        public const float  MatterPowerConversion = 10f;

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
            {Atmospherics.OxygenTransmitModifier,   Atmospherics.OxygenHeatPenalty,       Atmospherics.OxygenPowerMixRatio},   //oxygen
            {Atmospherics.NitrogenTransmitModifier, Atmospherics.NitrogenHeatPenalty,     Atmospherics.NitrogenPowerMixRatio}, //nitrogen
            {Atmospherics.CO2TransmitModifier,      Atmospherics.CO2TransmitModifier,     Atmospherics.CO2PowerMixRatio},      //co2
            {Atmospherics.PlasmaTransmitModifier,   Atmospherics.PlasmaTransmitModifier,  Atmospherics.PlasmaPowerMixRatio},   //plasma
            {Atmospherics.TritiumTransmitModifier,  Atmospherics.TritiumTransmitModifier, Atmospherics.TritiumPowerMixRatio},  //tritium
            {Atmospherics.WaterTransmitModifier,    Atmospherics.WaterTransmitModifier,   Atmospherics.WaterPowerMixRatio},    //water
        };
    }
}
