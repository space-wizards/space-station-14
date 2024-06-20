using Content.Server.Supermatter.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Damage.Prototypes;
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
    public static SoundSpecifier VaporizeSound = new SoundPathSpecifier("/Audio/Effects/gib2.ogg");

    [DataField("teslaSpawnPrototype")]
    public string TeslaPrototype = "";

    [DataField("singularitySpawnPrototype")]
    public string SingularityPrototype = "";

    [DataField("supermatterKudzuSpawnPrototype")]
    public string SupermatterKudzuPrototype = "";

    /// <summary>
    ///     Indicates whether supermatter crystal is active or not.
    /// </summary>
    [DataField("activated")]
    public bool Activated = false;

    public float UpdateTimerAccumulator = 0f;

    /// <summary>
    ///     Delta time between Update() calls storage.
    /// </summary>
    public float DeltaTime = 0f;

    [ViewVariables]
    public GasMixture AbsorbedGasMix = new();

    /// <summary>
    ///     Amount of seconds to pass before another SM cycle.
    /// </summary>
    [DataField("updateTimer")]
    public float UpdateTimer = 1f;

    /// <summary>
    ///     The time in seconds for crystal to delaminate.
    ///     60 seconds by default; with a sliver removed - 30.
    /// </summary>
    [DataField("countdownTimer")]
    public float CountdownTimerRaw = 60f;
    public float CountdownTimer => SliverRemoved ? CountdownTimerRaw / 2 : CountdownTimerRaw;

    /// <summary>
    ///     Lesser than that and it's not worth processing.
    /// </summary>
    public const float MinimumMoleCount = .01f;

    public const float
        HeatPenaltyThreshold = 40f,
        PowerPenaltyThreshold = 3f,
        MolePenaltyThreshold = 1800f,
        ThermalReleaseModifier = 4f,
        PlasmaReleaseModifier = 5f,
        OxygenReleaseModifier = 2.5f,
        GasHeatPowerScaling = 1f / 6f;

    /// <summary>
    ///     The portion of gasmix we should absorb.
    /// </summary>
    [DataField("gasAbsorptionRatio")]
    public float AbsorptionRatio = .15f;

    /// <summary>
    ///     This value effects gas output, damage and power generation.
    /// </summary>
    [ViewVariables]
    public float InternalEnergy = 0f;

    /// <summary>
    ///     The amount of damage the SM currently has.
    /// </summary>
    [DataField("damage")]
    public float Damage = 0f;

    /// <summary>
    ///     The temperature at which the supermatter crystal will begin to take damage.
    /// </summary>
    public float TempLimit = Atmospherics.T0C + HeatPenaltyThreshold;

    /// <summary>
    ///     Multiplies our gas waste amount and temperature.
    /// </summary>
    [ViewVariables]
    public float WasteMultiplier = 0f;

    [DataField("damageWarningPoint")]
    public float DamageWarningPoint = 10f;

    [DataField("damageDangerPoint")]
    public float DangerPoint = 50f;

    [DataField("damageEmergencyPoint")]
    public float EmergencyPoint = 75f;

    [DataField("damageDelaminationPoint")]
    public float DelaminationPoint = 100f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool AreWeDelaming = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public float DelamCountdownAccumulator = 0f;

    /// <summary>
    ///     A scaling value that affects the severity of explosions.
    /// </summary>
    [DataField("explosionPower")]
    public float ExplosionPower = 35f;

    /// <summary>
    ///     Affects the heat SM makes.
    /// </summary>
    [ViewVariables]
    public float GasHeatModifier = 0f;
    /// <summary>
    ///     Affters the minimum point at which SM takes damage.
    /// </summary>
    [ViewVariables]
    public float GasHeatResistance = 0f;
    /// <summary>
    ///     How much power decay is negated. Complete power decay negation at 1.
    /// </summary>
    [ViewVariables]
    public float GasPowerlossInhibition = 0f;
    /// <summary>
    ///     Affects the amount of power the main SM zap makes.
    /// </summary>
    [ViewVariables]
    public float PowerTransmissionRate = 0f;
    /// <summary>
    ///     Affects the power gain the SM experiences from heat.
    /// </summary>
    [ViewVariables]
    public float HeatPowerGeneration = 0f;

    /// <summary>
    ///     External power that is added over time instead of immediately.
    /// </summary>
    public float ExternalPowerTrickle = 0f;
    /// <summary>
    ///     External power that is added to the SM on next
    ///     <see cref="SupermatterSystem.ProcessAtmos(EntityUid, SupermatterComponent)"/> call.
    /// </summary>
    public float ExternalPowerImmediate = 0f;

    /// <summary>
    ///     External damage that is added to the SM on next
    ///     <see cref="SupermatterSystem.ProcessAtmos(EntityUid, SupermatterComponent)"/> call.
    /// </summary>
    public float ExternalDamageImmediate = 0f;

    /// <summary>
    ///     The power threshold required to transform the powerloss function into a linear function from a cubic function.
    /// </summary>
    public float PowerlossLinearThreshold = 0f;
    /// <summary>
    ///     The offset of the linear powerloss function set so the transition is differentiable.
    /// </summary>
    public float PowerlossLinearOffset = 0f;

    /// <summary>
    ///     If a supermatter sliver has been removed. Lowers the delamination countdown time.
    /// </summary>
    public bool SliverRemoved = false;

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
    [ViewVariables]
    public float PowerTransmissionRate;
    /// <summary>
    ///     Affects the heat SM makes.
    /// </summary>
    [ViewVariables]
    public float HeatModifier;
    /// <summary>
    ///     Affters the minimum point at which SM takes damage.
    /// </summary>
    [ViewVariables]
    public float HeatResistance;
    /// <summary>
    ///     Affects the power gain the SM experiences from heat.
    /// </summary>
    [ViewVariables]
    public float HeatPowerGeneration;
    /// <summary>
    ///     How much power decay is negated. Complete power decay negation at 1.
    /// </summary>
    [ViewVariables]
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
    Explosion,
    Tesla,
    Singularity,
    ResonanceCascade,
}
