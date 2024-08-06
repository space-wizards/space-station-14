using Content.Server.Temperature.Systems;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Temperature.Components;

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
    /// The amount of energy, in Joules, needed to heat this entity up by 1 Kelvin.
    /// <see cref="HeatCapacityDirty"/> handles tracking whether this is out of date given changes to the sources.
    /// </summary>
    /// <remarks>
    /// Assuming this was recently recalculated this should just be <see cref="BaseHeatCapacity"/> plus any additional sources of heat capacity.
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly), Access(typeof(TemperatureSystem), Other = AccessPermissions.None)]
    public float TotalHeatCapacity;

    /// <summary>
    /// Whether any sources of heat capacity for this entity have changed since the last time <see cref="TotalHeatCapacity"/> was calculated.
    /// If this is true the next attempt to fetch the heat capacity of this entity using <see cref="TemperatureSystem.GetHeatCapacity"/> will recalculate it.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), Access(typeof(TemperatureSystem), Other = AccessPermissions.None)]
    public bool HeatCapacityDirty = true; // Force the first fetch to calculate the total heat capacity.

    /// <summary>
    /// The number of times <see cref="TotalHeatCapacity"/> has been modified since it was last recalculated from scratch.
    /// If this reaches <see cref="HeatCapacityUpdateInterval"/> then <see cref="HeatCapacityDirty"/> will be set to mark the heat capacity for recalculation.
    /// Exists to prevent floating point errors from making the heat capacity drift to any significant degree.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), Access(typeof(TemperatureSystem), Other = AccessPermissions.None)]
    public byte HeatCapacityTouched = 0;

    /// <summary>
    /// The maximum number of times <see cref="TotalHeatCapacity"/> can be modified before it needs to be recalculated from scratch.
    /// </summary>
    public const byte HeatCapacityUpdateInterval = 16;

    /// <summary>
    /// The default heat capacity for this entity assuming that there are no other sources of heat capacity (such as specific heat * mass).
    /// </summary>
    [DataField, Access(typeof(TemperatureSystem))]
    public float BaseHeatCapacity = 0f;

    /// <summary>
    /// Additional heat capacity per kg of mass.
    /// </summary>
    [DataField, Access(typeof(TemperatureSystem))]
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
