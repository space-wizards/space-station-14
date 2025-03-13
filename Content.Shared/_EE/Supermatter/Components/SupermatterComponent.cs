using Content.Shared.Atmos;
using Content.Shared.DeviceLinking;
using Content.Shared.DoAfter;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._EE.Supermatter.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SupermatterComponent : Component
{
    #region Base

    /// <summary>
    /// The current status of the singularity, used for alert sounds and the monitoring console
    /// </summary>
    [DataField]
    public SupermatterStatusType Status = SupermatterStatusType.Inactive;

    /// <summary>
    /// The supermatter's internal gas storage
    /// </summary>
    [DataField]
    public GasMixture? GasStorage;

    [DataField]
    public Color LightColorNormal = Color.FromHex("#ffe000");

    [DataField]
    public Color LightColorDelam = Color.FromHex("#ffe000");

    #endregion

    #region Prototypes

    [DataField]
    public EntProtoId[] LightningPrototypes =
    {
        "SupermatterLightning",
        "SupermatterLightningCharged",
        "SupermatterLightningSupercharged"
    };

    [DataField]
    public EntProtoId SliverPrototype = "SupermatterSliver";

    [DataField]
    public EntProtoId SingularitySpawnPrototype = "Singularity";

    [DataField]
    public EntProtoId TeslaSpawnPrototype = "TeslaEnergyBall";

    // one day...
    // [DataField]
    // public EntProtoId KudzuSpawnPrototype = "SupermatterKudzu";

    [DataField]
    public EntProtoId AnomalyBluespaceSpawnPrototype = "AnomalyBluespace";

    [DataField]
    public EntProtoId AnomalyGravitySpawnPrototype = "AnomalyGravity";

    [DataField]
    public EntProtoId AnomalyPyroSpawnPrototype = "AnomalyPyroclastic";

    [DataField]
    public EntProtoId CollisionResultPrototype = "Ash";

    #endregion

    #region Sounds

    [DataField]
    public SoundSpecifier DustSound = new SoundPathSpecifier("/Audio/_EE/Supermatter/supermatter.ogg");

    [DataField]
    public SoundSpecifier DistortSound = new SoundPathSpecifier("/Audio/_EE/Supermatter/charge.ogg");

    [DataField]
    public SoundSpecifier PullSound = new SoundPathSpecifier("/Audio/_EE/Supermatter/marauder.ogg");

    [DataField]
    public SoundSpecifier CalmLoopSound = new SoundPathSpecifier("/Audio/_EE/Supermatter/calm.ogg");

    [DataField]
    public SoundSpecifier DelamLoopSound = new SoundPathSpecifier("/Audio/_EE/Supermatter/delamming.ogg");

    [DataField]
    public SoundSpecifier? CurrentSoundLoop;

    [DataField]
    public SoundSpecifier CalmAccent = new SoundCollectionSpecifier("SupermatterAccentNormal");

    [DataField]
    public SoundSpecifier DelamAccent = new SoundCollectionSpecifier("SupermatterAccentDelam");

    [DataField]
    public ProtoId<SpeechSoundsPrototype> StatusSilentSound = "SupermatterSilent";

    [DataField]
    public ProtoId<SpeechSoundsPrototype> StatusWarningSound = "SupermatterWarning";

    [DataField]
    public ProtoId<SpeechSoundsPrototype> StatusDangerSound = "SupermatterDanger";

    [DataField]
    public ProtoId<SpeechSoundsPrototype> StatusEmergencySound = "SupermatterEmergency";

    [DataField]
    public ProtoId<SpeechSoundsPrototype> StatusDelamSound = "SupermatterDelaminating";

    [DataField]
    public ProtoId<SpeechSoundsPrototype>? StatusCurrentSound;

    #endregion

    #region Processing

    /// <summary>
    /// The internal energy of the supermatter
    /// </summary>
    [DataField]
    public float Power;

    /// <summary>
    /// Takes the energy that supermatter collision generates and slowly turns it into actual power
    /// </summary>
    [DataField]
    public float MatterPower;

    /// <summary>
    /// Affects the amount of oxygen and plasma that is released during supermatter reactions, as well as the heat generated
    /// </summary>
    [DataField]
    public float HeatModifier;

    /// <summary>
    /// The percentage of the gas on the supermatter's tile that is absorbed each atmos tick.
    /// </summary>
    [DataField]
    public float GasEfficiency = 0.05f;

    /// <summary>
    /// Uses <see cref="PowerlossDynamicScaling"/> and <see cref="GasStorage"/> to lessen the effects of our powerloss functions
    /// </summary>
    [DataField]
    public float PowerlossInhibitor = 1;

    /// <summary>
    /// Based on CO2 percentage, this slowly moves between 0 and 1.
    /// We use it to calculate <see cref="PowerlossInhibitor"/>.
    /// </summary>
    [DataField]
    public float PowerlossDynamicScaling;

    /// <summary>
    /// Affects the amount of damage and minimum point at which the SM takes heat damage
    /// </summary>
    [DataField]
    public float DynamicHeatResistance = 1;

    /// <summary>
    /// More moles of gases are harder to heat than fewer, so let's scale heat damage around them
    /// </summary>
    [DataField]
    public float MoleHeatPenaltyThreshold;

    /// <summary>
    /// Modifier to damage taken during supermatter reactions, soothing the supermatter when a psychologist is nearby
    /// </summary>
    [DataField]
    public float PsyCoefficient;

    /// <summary>
    /// The chance for supermatter lightning to strike random coordinates instead of an entity
    /// </summary>
    [DataField]
    public float ZapHitCoordinatesChance = 0.75f;

    /// <summary>
    /// The lifetime of a supermatter-spawned anomaly.
    /// </summary>
    [DataField]
    public float AnomalyLifetime = 60f;

    /// <summary>
    /// The minimum distance from the supermatter that anomalies will spawn at
    /// </summary>
    [DataField]
    public float AnomalySpawnMinRange = 5f;

    /// <summary>
    /// The maximum distance from the supermatter that anomalies will spawn at
    /// </summary>
    [DataField]
    public float AnomalySpawnMaxRange = 10f;

    /// <summary>
    /// The chance for a bluespace anomaly to spawn when power or damage is high
    /// </summary>
    [DataField]
    public float AnomalyBluespaceChance = 150f;

    /// <summary>
    /// The chance for a gravity anomaly to spawn when power or damage is high, and the severe power penalty threshold is exceeded
    /// </summary>
    [DataField]
    public float AnomalyGravityChanceSevere = 150f;

    /// <summary>
    /// The chance for a gravity anomaly to spawn when power or damage is high
    /// </summary>
    [DataField]
    public float AnomalyGravityChance = 750f;

    /// <summary>
    /// The chance for a pyroclastic anomaly to spawn when power or damage is high, and the severe power penalty threshold is exceeded
    /// </summary>
    [DataField]
    public float AnomalyPyroChanceSevere = 375f;

    /// <summary>
    /// The chance for a pyroclastic anomaly to spawn when power or damage is high, and the power penalty threshold is exceeded
    /// </summary>
    [DataField]
    public float AnomalyPyroChance = 2500f;

    #endregion

    #region Timing

    /// <summary>
    /// We yell if over 50 damage every YellTimer Seconds
    /// </summary>
    [DataField]
    public TimeSpan YellTimer;

    /// <summary>
    /// Last time the supermatter's damage was announced
    /// </summary>
    [DataField]
    public TimeSpan YellLast;

    /// <summary>
    /// Time when the delamination will occur
    /// </summary>
    [DataField]
    public TimeSpan DelamEndTime;

    /// <summary>
    /// How long it takes in seconds for the supermatter to delaminate after reaching zero integrity
    /// </summary>
    [DataField]
    public float DelamTimer = 30f;

    /// <summary>
    /// Last time a supermatter accent sound was triggered
    /// </summary>
    [DataField]
    public TimeSpan AccentLastTime;

    /// <summary>
    /// Minimum time in seconds between supermatter accent sounds
    /// </summary>
    [DataField]
    public float AccentMinCooldown = 2f;

    [DataField]
    public TimeSpan ZapLast;

    #endregion

    #region Damage

    /// <summary>
    /// The amount of damage taken
    /// </summary>
    [DataField]
    public float Damage = 0f;

    /// <summary>
    /// The damage from before this cycle.
    /// Used to limit the damage we can take each cycle, and for safe alert.
    /// </summary>
    [DataField]
    public float DamageArchived = 0f;

    /// <summary>
    /// Is multiplied by ExplosionPoint to cap evironmental damage per cycle
    /// </summary>
    [DataField]
    public float DamageHardcap = 0.002f;

    /// <summary>
    /// Environmental damage is scaled by this
    /// </summary>
    [DataField]
    public float DamageIncreaseMultiplier = 0.25f;

    /// <summary>
    /// Max space damage the SM will take per cycle
    /// </summary>
    [DataField]
    public float MaxSpaceExposureDamage = 2;

    /// <summary>
    /// The point at which we should start sending radio messages about the damage.
    /// </summary>
    [DataField]
    public float DamageWarningThreshold = 50;

    /// <summary>
    /// The point at which we start sending station announcements about the damage.
    /// </summary>
    [DataField]
    public float DamageEmergencyThreshold = 500;

    /// <summary>
    /// The point at which the SM begins shooting lightning.
    /// </summary>
    [DataField]
    public int DamagePenaltyPoint = 550;

    /// <summary>
    /// The point at which the SM begins delaminating.
    /// </summary>
    [DataField]
    public int DamageDelaminationPoint = 900;

    /// <summary>
    /// The point at which the SM begins showing warning signs.
    /// </summary>
    [DataField]
    public int DamageDelamAlertPoint = 300;

    [DataField]
    public bool Delamming;

    [DataField]
    public DelamType PreferredDelamType = DelamType.Explosion;

    #endregion

    #region Announcements

    [DataField]
    public bool DelamAnnounced;

    /// <summary>
    /// The radio channel for supermatter alerts
    /// </summary>
    [DataField]
    public bool SuppressAnnouncements = false;


    /// <summary>
    /// The radio channel for supermatter alerts
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> Channel = "Engineering";

    /// <summary>
    /// The common radio channel for severe supermatter alerts
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> ChannelGlobal = "Common";

    /// <summary>
    /// Used for logging if the supermatter has been powered
    /// </summary>
    [DataField]
    public bool HasBeenPowered;

    #endregion

    #region Signal Ports

    [DataField]
    public ProtoId<SourcePortPrototype> PortInactive = "SupermatterInactive";

    [DataField]
    public ProtoId<SourcePortPrototype> PortNormal = "SupermatterNormal";

    [DataField]
    public ProtoId<SourcePortPrototype> PortCaution = "SupermatterCaution";

    [DataField]
    public ProtoId<SourcePortPrototype> PortWarning = "SupermatterWarning";

    [DataField]
    public ProtoId<SourcePortPrototype> PortDanger = "SupermatterDanger";

    [DataField]
    public ProtoId<SourcePortPrototype> PortEmergency = "SupermatterEmergency";

    [DataField]
    public ProtoId<SourcePortPrototype> PortDelaminating = "SupermatterDelaminating";

    #endregion

    #region Console-Only Values

    /// <summary>
    /// The power decay of the supermatter, to be displayed on the monitoring console
    /// </summary>
    [DataField]
    public float PowerLoss;

    /// <summary>
    /// The low temperature healing of the supermatter, to be displayed on the monitoring console
    /// </summary>
    [DataField]
    public float HeatHealing;

    /// <summary>
    /// The true value of <see cref="HeatModifier"/> without a lower bound, to be displayed on the monitoring console
    /// </summary>
    [DataField]
    public float GasHeatModifier;

    #endregion
}

public enum DelamType : int
{
    Explosion = 0,
    Singulo = 1,
    Tesla = 2,
    Cascade = 3
}

[Serializable, NetSerializable]
public struct SupermatterGasFact
{
    /// <summary>
    /// Multiplied with the supermatter's power to determine rads
    /// </summary>
    public float TransmitModifier;

    /// <summary>
    /// Affects the amount of oxygen and plasma that is released during supermatter reactions, as well as the heat generated
    /// </summary>
    public float HeatPenalty;

    /// <summary>
    /// Affects the amount of power generated by the supermatter
    /// </summary>
    public float PowerMixRatio;

    /// <summary>
    /// Affects the supermatter's resistance to temperature
    /// </summary>
    public float HeatResistance;

    public SupermatterGasFact(float transmitModifier, float heatPenalty, float powerMixRatio, float heatResistance)
    {
        TransmitModifier = transmitModifier;
        HeatPenalty = heatPenalty;
        PowerMixRatio = powerMixRatio;
        HeatResistance = heatResistance;
    }
}

[Serializable, NetSerializable]
public static class SupermatterGasData
{
    public static readonly Dictionary<Gas, SupermatterGasFact> GasData = new()
    {
        { Gas.Oxygen,        new(1.5f, 1f,    1f,  1f) },
        { Gas.Nitrogen,      new(0f,   -1.5f, -1f, 1f) },
        { Gas.CarbonDioxide, new(0f,   0.1f,  1f,  1f) },
        { Gas.Plasma,        new(4f,   15f,   1f,  1f) },
        { Gas.Tritium,       new(30f,  10f,   1f,  1f) },
        { Gas.WaterVapor,    new(2f,   12f,   1f,  1f) },
        { Gas.Ammonia,       new(0f,   1f,    1f , 1f) },
        { Gas.NitrousOxide,  new(0f,   -5f,   -1f, 6f) },
        { Gas.Frezon,        new(3f,   -10f,  -1f, 1f) }
    };

    public static float CalculateGasMixModifier(GasMixture mix, Func<SupermatterGasFact, float> getModifier)
    {
        var modifier = 0f;

        foreach (var gasId in Enum.GetValues<Gas>())
            modifier += mix.GetMoles(gasId) * getModifier(GasData.GetValueOrDefault(gasId));

        return modifier;
    }

    public static float GetTransmitModifiers(GasMixture mix)
    {
        return CalculateGasMixModifier(mix, data => data.TransmitModifier);
    }

    public static float GetHeatPenalties(GasMixture mix)
    {
        return CalculateGasMixModifier(mix, data => data.HeatPenalty);
    }

    public static float GetPowerMixRatios(GasMixture mix)
    {
        return CalculateGasMixModifier(mix, data => data.PowerMixRatio);
    }

    public static float GetHeatResistances(GasMixture mix)
    {
        return CalculateGasMixModifier(mix, data => data.HeatResistance);
    }
}

[Serializable, NetSerializable]
public enum SupermatterStatusType : sbyte
{
    Error = -1,
    Inactive = 0,
    Normal = 1,
    Caution = 2,
    Warning = 3,
    Danger = 4,
    Emergency = 5,
    Delaminating = 6
}

[Serializable, NetSerializable]
public enum SupermatterCrystalState : byte
{
    Normal,
    Glow,
    GlowEmergency,
    GlowDelam
}

[Serializable, NetSerializable]
public enum SupermatterVisuals : byte
{
    Crystal,
    Psy
}

[Serializable, NetSerializable]
public sealed partial class SupermatterDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class SupermatterSpriteUpdateEvent(NetEntity uid, string state) : EntityEventArgs
{
    public NetEntity Entity = uid;
    public string State = state;
}
