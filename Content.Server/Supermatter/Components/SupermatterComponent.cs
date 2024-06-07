using Content.Server.Supermatter.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Server.Supermatter.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SupermatterComponent : Component
{
    [DataField]
    public EntityWhitelist Whitelist = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public float Power = 0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float GasmixPowerRatio { get; set; } = 0;

    [DataField]
    public float DynamicHeatModifier { get; set; } = 0;

    [DataField]
    public float PowerTransmissionBonus { get; set; } = 0;

    [DataField]
    public float PowerlossDynamicScaling { get; set; } = 0;

    [DataField]
    public float DynamicHeatResistance { get; set; } = 0;

    [DataField]
    public float PowerlossInhibitor { get; set; } = 0;

    public float MoleHeatPenalty { get; set; } = 0f;

    //The damage we had before this cycle. Used to limit the damage we can take each cycle, and for safealert
    public float DamageArchived { get; set; } = 0;

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

    //we yell if over 50 damage every YellTimer Seconds
    public const float YellTimer = 60f;


    //set to YellTimer at first so it doesnt yell a minute after being hit
    public float YellAccumulator { get; set; } = YellTimer;

    //it's the final countdown
    [ViewVariables(VVAccess.ReadOnly)]
    public bool FinalCountdown { get; set; } = false;

    public float DamageUpdateAccumulator { get; set; }
    //update environment damage every second
    public const float DamageUpdateTimer = 1f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float DelamTimerAccumulator { get; set; }
    public const int DelamTimerTimer = 30;
    public float SpeakAccumulator { get; set; } = 5f;
    public float AtmosUpdateAccumulator { get; set; }
    //update atmos every half second
    public const float AtmosUpdateTimer = 0.5f;

    //---------------------------------------------------------------------------------------\\

    //Higher == Higher percentage of inhibitor gas needed before the charge inertia chain reaction effect starts.
    public const float PowerlossInhibitionGasThreshold = 0.20f;
    //Higher == More moles of the gas are needed before the charge inertia chain reaction effect starts.
    //Scales powerloss inhibition down until this amount of moles is reached
    public const float PowerlossInhibitionMoleThreshold = 20f;
    //bonus powerloss inhibition boost if this amount of moles is reached
    public const float PowerlossInhibitionMoleBoostThreshold = 500f;

    //---------------------------------------------------------------------------------------\\

    //Above this value we can get lord singulo and independent mol damage, below it we can heal damage
    public const float MolePenaltyThreshold = 1800f;

    //---------------------------------------------------------------------------------------\\

    //Along with damagepenaltypoint, makes flux anomalies.
    /// The cutoff for the minimum amount of power required to trigger the crystal invasion delamination event.
    public const float EventPowerPenaltyThreshold = 4500f;
    //The cutoff on power properly doing damage, pulling shit around, and delamming into a tesla. Low chance of pyro anomalies, +2 bolts of electricity
    public const float PowerPenaltyThreshold = 5000f;
    //+1 bolt of electricity, allows for gravitational anomalies, and higher chances of pyro anomalies
    public const float SeverePowerPenaltyThreshold = 7000f;
    //+1 bolt of electricity.
    public const float CriticalPowerPenaltyThreshold = 9000f;

    //---------------------------------------------------------------------------------------\\

    //Higher == Crystal safe operational temperature is higher.
    public const float HeatPenaltyThreshold = 40f;
    //is multiplied by ExplosionPoint to cap evironmental damage per cycle
    public const float DamageHardcap = 0.002f;
    //environmental damage is scaled by this
    public const float DamageIncreaseMultiplier = 0.25f;
    //if spaced sm wont take more than 2 damage per cycle
    public const float MaxSpaceExposureDamage = 2;

    //Higher == more overall power
    public const float ReactionPowerModefier = 0.55f;

    //These would be what you would get at point blank, decreases with distance
    public const float DetonationRads = 200f;
    public const float DetonationHallucination = 600f;

    // always make it match with the gases from Shared.Atmos
    [DataField]
    public GasFact[] GasFacts =
    {
        new GasFact(transmitModifier: 1.5f, penalty: 1f,    ratio: 1f, releaseModifier: 325f), // o2
        new GasFact(transmitModifier: 0f,   penalty: -1.5f, ratio: -1f), // n2
        new GasFact(transmitModifier: 0f,   penalty: 0.1f,  ratio: 1f), // co2
        new GasFact(transmitModifier: 4f,   penalty: 15f,   ratio: 1f), // plas
        new GasFact(transmitModifier: 30f,  penalty: 10f,   ratio: 1f), // trit
        new GasFact(transmitModifier: 2f,   penalty: 12f,   ratio: 1f), // vapor
        new GasFact(transmitModifier: 1f,   penalty: 5f,    ratio: 1f), // ommonium
        new GasFact(transmitModifier: 1f,   penalty: 1f,    ratio: 1f, heatResistance: 5f), // n2o
        new GasFact(transmitModifier: -5f,  penalty: -3.5f, ratio: -1f), // freon
    };

    public float ThermalReleaseModifier = 5f;

    public struct GasFact
    {
        /// <summary>
        ///     Gives a bonus to the power generation, the higher - the better.
        /// </summary>
        public float TransmitModifier;

        /// <summary>
        ///     Controls the amount of whatever's being released. The higher - the less.
        /// </summary>
        public float ReleaseModifier;

        /// <summary>
        ///     Heat and waste penalty from having the crystal surrounded by whatever gas.
        ///     The higher - the better.
        /// </summary>
        public float Penalty;

        /// <summary>
        ///     If it's negative - it reduces.
        /// </summary>
        public float Ratio;

        /// <summary>
        ///     Provides heat resistance, the more - the better.
        /// </summary>
        public float HeatResistance;

        public GasFact(float? transmitModifier = null, float? penalty = null, float? ratio = null, float? releaseModifier = null, float? heatResistance = null)
        {
            TransmitModifier = transmitModifier ?? 1;
            Penalty = penalty ?? 0;
            Ratio = ratio ?? 1;
            ReleaseModifier = releaseModifier ?? 0;
            HeatResistance = heatResistance ?? 0;
        }
    }

    public enum DelamType : sbyte
    {
        Explosion,
        Tesla,
        Singularity,
        ResonanceCascade, // save for whenever anti and hyper nobilium gases get added
    }
}
