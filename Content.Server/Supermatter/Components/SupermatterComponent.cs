using Content.Shared.Atmos;
using Robust.Shared.Audio;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterComponent : Component
{
    /// <summary>
    ///     Lightning prototype IDs that the supermatter should spit out.
    /// </summary>
    public readonly string[] LightningPrototypeIDs =
    {
        "Lightning",
        "ChargedLightning",
        "SuperchargedLightning",
        "HyperchargedLightning"
    };
    public readonly string SliverPrototype = "SupermatterSliver";

    [DataField("zapSound")]
    public static SoundSpecifier SupermatterZapSound = new SoundPathSpecifier("/Audio/Weapons/emitter2.ogg");

    [DataField("calmAmbienceSound")]
    public SoundSpecifier CalmAmbienceSound = new SoundPathSpecifier("/Audio/Ambience/Objects/supermatter_calm.ogg");

    [DataField("delamAmbienceSound")]
    public SoundSpecifier DelamAmbienceSound = new SoundPathSpecifier("/Audio/Ambience/Objects/supermatter_delam.ogg");

    [ViewVariables]
    public SoundSpecifier CurrentAmbience = new SoundPathSpecifier("/Audio/Ambience/Objects/supermatter_calm.ogg");

    [DataField("vaporizeSound")]
    public static SoundSpecifier VaporizeSound = new SoundPathSpecifier("/Audio/Effects/Grenades/Supermatter/supermatter_start.ogg");

    [DataField("teslaSpawnPrototype")]
    public string TeslaPrototype = "TeslaEnergyBall";

    [DataField("singularitySpawnPrototype")]
    public string SingularityPrototype = "Singularity";

    [DataField("supermatterKudzuSpawnPrototype")]
    public string SupermatterKudzuPrototype = "SupermatterKudzu";

    /// <summary>
    ///     If a supermatter sliver has been removed. Lowers the delamination countdown time.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool SliverRemoved = false;

    /// <summary>
    ///     Indicates whether supermatter crystal is active or not.
    /// </summary>
    [DataField("activated")]
    public bool Activated = false;

    [ViewVariables]
    public GasMixture AbsorbedGasMix = new();

    /// <summary>
    ///     Delta time between Update() calls storage.
    /// </summary>
    public float DeltaTime = 0f;

    public float UpdateTimerAccumulator = 0f;

    public float ZapTimerAccumulator = 0f;

    public float AnnouncementTimerAccumulator = 0f;

    /// <summary>
    ///     Amount of seconds to pass before another SM cycle.
    /// </summary>
    [DataField("updateTimer")]
    public float UpdateTimer = 1f;

    /// <summary>
    ///     Amount of seconds to pass before SM does it's zap.
    /// </summary>
    [DataField("zapTimer")]
    public float ZapTimer = 10f;

    /// <summary>
    ///     Amount of seconds to pass before makes an announcement.
    /// </summary>
    [DataField("announcementTimer")]
    public float AnnouncementTimer = 60f;

    [ViewVariables(VVAccess.ReadWrite)]
    public int? PreferredDelamType = 0;

    /// <summary>
    ///     The time in seconds for crystal to delaminate.
    /// </summary>
    [DataField("countdownTimer")]
    public float DelamCountdownTimerRaw = 120f;
    public float DelamCountdownTimer => SliverRemoved ? DelamCountdownTimerRaw / 2 : DelamCountdownTimerRaw;
    public bool DelamAnnouncementHappened = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public float DelamCountdownAccumulator = 0f;

    /// <summary>
    ///     The portion of gasmix we should absorb.
    /// </summary>
    [DataField("gasAbsorptionRatio")]
    public float AbsorptionRatio = .15f;

    /// <summary>
    ///     This value effects gas output, damage and power generation.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float InternalEnergy = 0f;

    /// <summary>
    ///     The amount of damage the SM currently has.
    /// </summary>
    [DataField("damage")]
    public float Damage = 0f;

    /// <summary>
    ///     The amount of damage SM had before the cycle.
    /// </summary>
    public float DamageArchive = 0f;

    /// <summary>
    ///     The damage taken from direct hits, e.g. laser weapons
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float ExternalDamage = 0f;

    /// <summary>
    ///     The temperature at which the supermatter crystal will begin to take damage.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float TempLimit = Atmospherics.T0C + HeatPenaltyThreshold;

    /// <summary>
    ///     Multiplies our gas waste amount and temperature.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float WasteMultiplier = 0f;

    [DataField("damageDangerPoint")]
    public float DamageDangerPoint = 50f;

    [DataField("damageEmergencyPoint")]
    public float DamageEmergencyPoint = 75f;

    [DataField("damageDelaminationPoint")]
    public float DelaminationPoint = 100f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool AreWeDelaming = false;

    /// <summary>
    ///     Affects the heat SM makes.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float GasHeatModifier = 0f;
    /// <summary>
    ///     Affters the minimum point at which SM takes damage.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float GasHeatResistance = 0f;
    /// <summary>
    ///     How much power decay is negated. Complete power decay negation at 1.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float GasPowerlossInhibition = 0f;
    /// <summary>
    ///     Affects the amount of power the main SM zap makes.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float PowerTransmissionRate = 0f;
    /// <summary>
    ///     Affects the power gain the SM experiences from heat.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float HeatPowerGeneration = 0f;

    /// <summary>
    ///     Lesser than that and it's not worth processing.
    /// </summary>
    public const float MinimumMoleCount = .01f;

    public const float HeatPenaltyThreshold = 40f;
    public const float PowerPenaltyThreshold = 3f;
    public const float MolePenaltyThreshold = 900f;
    public const float ThermalReleaseModifier = 4f;
    public const float PlasmaReleaseModifier = 1.5f;
    public const float OxygenReleaseModifier = 2.5f;
    public const float GasHeatPowerScaling = 1f / 6f;

    /// <summary>
    ///     Stores gas properties used for the supermatter.
    ///     Array values should alwayd match gas enum values.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public GasFact[] GasFacts =
    {
        new (transmissionRate: .15f, heatPowerGeneration: 1f), // o2
        new (heatModifier: -2.5f, heatPowerGeneration: -1), // n2
        new (heatModifier: 1f, heatPowerGeneration: 1f, powerlossInhibition: 1f), // co2
        new (transmissionRate: .4f, heatModifier: 14f, heatPowerGeneration: 1f), // plasma
        new (transmissionRate: 3f, heatModifier: 9f, heatPowerGeneration: 1f), // tritium
        new (transmissionRate: -.25f, heatModifier: 11f, heatPowerGeneration: 1f), // vapor
        new (heatPowerGeneration: .5f), // ommonium
        new (heatResistance: 5f), // n2o
        new (transmissionRate: -3f, heatModifier: 9f, heatResistance: 1f, heatPowerGeneration: 1f), // frezon
    };
}

/// <summary>
///     Stores gas properties used for the supermatter.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class GasFact
{
    /// <summary>
    ///     Affects the amount of power the main SM zap makes.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float PowerTransmissionRate;
    /// <summary>
    ///     Affects the heat SM makes.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float HeatModifier;
    /// <summary>
    ///     Affters the minimum point at which SM takes damage.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float HeatResistance;
    /// <summary>
    ///     Affects the power gain the SM experiences from heat.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float HeatPowerGeneration;
    /// <summary>
    ///     How much power decay is negated. Complete power decay negation at 1.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float PowerlossInhibition;

    public GasFact(float? transmissionRate = null, float? heatModifier = null, float? heatResistance = null, float? heatPowerGeneration = null, float? powerlossInhibition = null)
    {
        PowerTransmissionRate = transmissionRate ?? 1;
        HeatModifier = heatModifier ?? 1;
        HeatResistance = heatResistance ?? 0;
        HeatPowerGeneration = heatPowerGeneration ?? 0;
        PowerlossInhibition = powerlossInhibition ?? 0;
    }
}

/// <summary>
///     Type of delamination that should occur.
/// </summary>
public enum DelamType : sbyte
{
    Explosion = 0,
    Tesla = 1,
    Singularity = 2,
    ResonanceCascade = 3,
}
