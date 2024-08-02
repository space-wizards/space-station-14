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
    /// The total heat capacity of this entity given all available sources.
    /// <see cref="HeatCapacityDirty"/> handles tracking whether this is out of date given changes to the sources. 
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), Access(typeof(TemperatureSystem), Other = AccessPermissions.None)]
    public float TotalHeatCapacity;

    /// <summary>
    /// Whether any sources of heat capacity for this entity have changed since the last time <see cref="TotalHeatCapacity"/> was calculated.
    /// If this is true the next attempt to fetch the heat capacity of this entity using <see cref="TemperatureSystem.GetHeatCapacity"/> will recalculate it.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), Access(typeof(TemperatureSystem), Other = AccessPermissions.None)]
    public bool HeatCapacityDirty = true; // Force the first fetch to calculate the total heat capacity.

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

    [Obsolete("Use system method")]
    public float HeatCapacity
    {
        get
        {
            return IoCManager.Resolve<IEntityManager>().System<TemperatureSystem>().GetHeatCapacity(Owner, this);
        }
    }

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier ColdDamage = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier HeatDamage = new();

    /// <summary>
    /// Temperature won't do more than this amount of damage per second.
    /// </summary>
    /// <remarks>
    /// Okay it genuinely reaches this basically immediately for a plasma fire.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 DamageCap = FixedPoint2.New(8);

    /// <summary>
    /// Used to keep track of when damage starts/stops. Useful for logs.
    /// </summary>
    [DataField]
    public bool TakingDamage = false;

    [DataField]
    public ProtoId<AlertPrototype> HotAlert = "Hot";

    [DataField]
    public ProtoId<AlertPrototype> ColdAlert = "Cold";
}
