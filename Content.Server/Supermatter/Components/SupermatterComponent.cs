using Content.Server.Supermatter.EntitySystems;
using Robust.Shared.GameStates;
using Content.Shared.Atmos;
using Robust.Shared.Audio;

namespace Content.Server.Supermatter.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SupermatterComponent : Component
{
    /// <summary>
    ///     Lightning prototype IDs that the supermatter should spit out.
    ///     From 0 (LightningRevenant) to 3 (HyperchargedLightning).
    /// </summary>
    public readonly string[] LightningPrototypeIDs =
    {
        "LightningRevenant",
        "ChargedLightning",
        "SuperchargedLightning",
        "HyperchargedLightning"
    };
    public readonly string SliverPrototype = "SupermatterSliver";
    public readonly SoundSpecifier
        SupermatterZapSound = new SoundPathSpecifier("/Audio/Weapons/emitter2.ogg"),
        CalmAmbienceSound = new SoundPathSpecifier("/Audio/Ambience/Objects/supermatter_calm.ogg"),
        DelamAmbienceSound = new SoundPathSpecifier("/Audio/Ambience/Objects/supermatter_calm.ogg");
    public SoundSpecifier CurrentAmbience = new SoundPathSpecifier("/Audio/Ambience/Objects/supermatter_calm.ogg");

    /// <summary>
    ///     Indicates whether supermatter crystal is active or not.
    /// </summary>
    [DataField]
    public bool Activated = false;

    public float UpdateTimerAccumulator = 0f;

    public GasMixture AbsorbedGasMix = null;

    public DelamType PreferedDelamination = DelamType.Explosion;

    #region Constants
    /// <summary>
    ///     Amount of seconds to pass before SM goes at it again.
    /// </summary>
    public readonly float UpdateTimer = 1f;

    /// <summary>
    ///     The time in seconds for crystal to delaminate.
    ///     60 seconds by default; with a sliver removed - 30.
    /// </summary>
    public float CountdownTimer => SliverRemoved ? 30f : 60f;

    /// <summary>
    ///     Lesser than that and it's not worth processing.
    /// </summary>
    public const float MinimumMoleCount = .01f;

    public const float
        BasePowerTransmissionRate = 1040,
        HeatPenaltyThreshold = 40,
        PowerPenaltyThreshold = 5000,
        MolePenaltyThreshold = 1800,
        ReactionPowerModifier = .65f,
        ThermalReleaseModifier = 4,
        PlasmaReleaseModifier = 650,
        OxygenReleaseModifier = 340,
        GasHeatPowerScaling = 1 / 6;

    // localised strings for "future i guess" :godo:
    public const string
        SmPowerExternalImmediate = "External Power Gain",
        SmPowerExternalTrickle = "External Power Trickle",
        SmPowerHeat = "Gas Heat Power Gain",

        SmPowerloss = "Internal Power Decay",
        SmPowerlossGas = "Gas Power Decay Negation",

        SmZapBase = "Base Zap Transmission",
        SmZapGas = "Gas Zap Transmission Modifier",

        SmTempLimitBase = "Base Heat Resistance",
        SmTempLimitGas = "Gas Heat Resistance",
        SmTempLimitMoles = "Low Moles Heat Resistance",

        SmDamageExternal = "External Damage",
        SmDamageHeat = "Heat Damage",
        SmDamagePower = "Charge Damage",
        SmDamageMoles = "Molar Damage",
        SmDamageHealHeat = "Low Temperature Healing",

        SmWasteBase = "Base Waste Multiplier",
        SmWasteGas = "Gas Waste Multiplier";
    #endregion

    /// <summary>
    ///     The portion of gasmix we should absorb.
    /// </summary>
    public float AbsorptionRatio = .15f;

    /// <summary>
    ///     This value effects gas output, damage and power generation.
    /// </summary>
    public float InternalEnergy = 0f;

    /// <summary>
    ///     The amount of damage the SM currently has.
    /// </summary>
    public float Damage = 0f;
    /// <summary>
    ///     Integrity used for announcements.
    /// </summary>
    public float Integrity => 100 - Damage;
    /// <summary>
    ///     The damage we had before the cycle.
    ///     Used to check if we're currently hurting or healing.
    /// </summary>
    public float DamageArchived = 0f;

    /// <summary>
    ///     The zap power transmission over internal energy. W/MeV
    /// </summary>
    public float ZapTransmissionRate = BasePowerTransmissionRate;

    /// <summary>
    ///     The temperature at which the supermatter crystal will begin to take damage.
    /// </summary>
    public float TempLimit = Atmospherics.T0C + HeatPenaltyThreshold;

    /// <summary>
    ///     Multiplies our gas waste amount and temperature.
    /// </summary>
    public float WasteMultiplier = 0;

    [DataField]
    public float DamageWarningPoint = 10;

    [DataField]
    public float DangerPoint = 50;

    [DataField]
    public float EmergencyPoint = 75;

    [DataField]
    public float DelaminationPoint = 100;

    public bool AreWeDelaming = false;

    /// <summary>
    ///     A scaling value that affects the severity of explosions.
    /// </summary>
    [DataField]
    public float ExplosionPower = 35f;

    /// <summary>
    ///     Affects the heat SM makes.
    /// </summary>
    public float GasHeatModifier = 0f;
    /// <summary>
    ///     Affters the minimum point at which SM takes damage.
    /// </summary>
    public float GasHeatResistance = 0f;
    /// <summary>
    ///     How much power decay is negated. Complete power decay negation at 1.
    /// </summary>
    public float GasPowerlossInhibition = 0f;
    /// <summary>
    ///     Affects the amount of power the main SM zap makes.
    /// </summary>
    public float PowerTransmissionRate = 0f;
    /// <summary>
    ///     Affects the power gain the SM experiences from heat.
    /// </summary>
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
        new (heatResistance: 5), // n2o
        new (transmissionRate: -3f, heatModifier: 9f, heatResistance: 1f, heatPowerGeneration: 1f), // frezon
    };
}

/// <summary>
///     Stores gas properties used for the supermatter.
/// </summary>
public struct GasFact
{
    /// <summary>
    ///     Affects the amount of power the main SM zap makes.
    /// </summary>
    public float PowerTransmissionRate;
    /// <summary>
    ///     Affects the heat SM makes.
    /// </summary>
    public float HeatModifier;
    /// <summary>
    ///     Affters the minimum point at which SM takes damage.
    /// </summary>
    public float HeatResistance;
    /// <summary>
    ///     Affects the power gain the SM experiences from heat.
    /// </summary>
    public float HeatPowerGeneration;
    /// <summary>
    ///     How much power decay is negated. Complete power decay negation at 1.
    /// </summary>
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
    Explosion, // a big explosion. really big. even bigger than syndie bomb.
    Tesla, // tesla with like 8 mini other teslas.
    Singularity, // instant level 6 singuloose. people are so screwed.
    ResonanceCascade, // total crew death. // save for whenever anti and hyper nobilium gases get added
}
