using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Handles changing temperature,
/// informing others of the current temperature,
/// and taking fire damage from high temperature.
/// </summary>
[RegisterComponent]
public sealed partial class TemperatureComponent : Component
{
    /// <summary>
    /// Surface temperature which is modified by the environment.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CurrentTemperature = Atmospherics.T20C;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HeatDamageThreshold = 360f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ColdDamageThreshold = 260f;

    /// <summary>
    /// Overrides HeatDamageThreshold if the entity's within a parent with the TemperatureDamageThresholdsComponent component.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float? ParentHeatDamageThreshold;

    /// <summary>
    /// Overrides ColdDamageThreshold if the entity's within a parent with the TemperatureDamageThresholdsComponent component.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float? ParentColdDamageThreshold;

    /// <summary>
    /// Heat capacity per kg of mass.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SpecificHeat = 50f;

    /// <summary>
    /// How well does the air surrounding you merge into your body temperature?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AtmosTemperatureTransferEfficiency = 0.1f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier ColdDamage = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier HeatDamage = new();

    /// <summary>
    /// Temperature won't do more than this amount of damage per second.
    /// </summary>
    /// <remarks>
    /// Okay it genuinely reaches this basically immediately for a plasma fire.
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 DamageCap = FixedPoint2.New(8);

    /// <summary>
    /// Used to keep track of when damage starts/stops. Useful for logs.
    /// </summary>
    [DataField]
    public bool TakingDamage;

    [DataField]
    public ProtoId<AlertPrototype> HotAlert = "Hot";

    [DataField]
    public ProtoId<AlertPrototype> ColdAlert = "Cold";
}
