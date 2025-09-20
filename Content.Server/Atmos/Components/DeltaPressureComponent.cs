using Content.Server.Atmos.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Guidebook;

namespace Content.Server.Atmos.Components;

/// <summary>
/// Entities that have this component will have damage done to them depending on the local pressure
/// environment that they reside in.
///
/// Atmospherics.DeltaPressure batch-processes entities with this component in a list on
/// the grid's <see cref="GridAtmosphereComponent"/>.
/// The entities are automatically added and removed from this list, and automatically
/// added on initialization.
/// </summary>
/// <remarks> Note that the entity should have an <see cref="AirtightComponent"/> and be a grid structure.</remarks>
[RegisterComponent]
public sealed partial class DeltaPressureComponent : Component
{
    /// <summary>
    /// Whether the entity is currently in the processing list of the grid's <see cref="GridAtmosphereComponent"/>.
    /// </summary>
    [DataField(readOnly: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(DeltaPressureSystem), typeof(AtmosphereSystem))]
    public bool InProcessingList;

    /// <summary>
    /// Whether this entity is currently taking damage from pressure.
    /// </summary>
    [DataField(readOnly: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(DeltaPressureSystem), typeof(AtmosphereSystem))]
    public bool IsTakingDamage;

    /// <summary>
    /// The grid this entity is currently joined to for processing.
    /// Required for proper deletion, as we cannot reference the grid
    /// for removal while the entity is being deleted.
    /// </summary>
    /// <remarks>Note that while <see cref="AirtightComponent"/> already stores the grid,
    /// we cannot trust it to be available on init or when the entity is being deleted. Tragic.</remarks>
    [DataField]
    public EntityUid? GridUid;

    /// <summary>
    /// The percent chance that the entity will take damage each atmos tick,
    /// when the entity is above the damage threshold.
    /// Makes it so that windows don't all break in one go.
    /// Float is from 0 to 1, where 1 means 100% chance.
    /// If this is set to 0, the entity will never take damage.
    /// </summary>
    [DataField]
    public float RandomDamageChance = 1f;

    /// <summary>
    /// The base damage applied to the entity per atmos tick when it is above the damage threshold.
    /// This damage will be scaled as defined by the <see cref="DeltaPressureDamageScalingType"/> enum
    /// depending on the current effective pressure this entity is experiencing.
    /// Note that this damage will scale depending on the pressure above the minimum pressure,
    /// not at the current pressure.
    /// </summary>
    [DataField]
    public DamageSpecifier BaseDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Structural", 10 },
        },
    };

    /// <summary>
    /// The minimum pressure in kPa at which the entity will start taking damage.
    /// This doesn't depend on the difference in pressure.
    /// The entity will start to take damage if it is exposed to this pressure.
    /// This is needed because we don't correctly handle 2-layer windows yet.
    /// </summary>
    [DataField]
    public float MinPressure = 10000;

    /// <summary>
    /// The minimum difference in pressure between any side required for the entity to start taking damage.
    /// </summary>
    [DataField]
    [GuidebookData]
    public float MinPressureDelta = 7500;

    /// <summary>
    /// The maximum pressure at which damage will no longer scale.
    /// If the effective pressure goes beyond this, the damage will be considered at this pressure.
    /// </summary>
    [DataField]
    public float MaxEffectivePressure = 10000;

    /// <summary>
    /// Simple constant to affect the scaling behavior.
    /// See comments in the <see cref="DeltaPressureDamageScalingType"/> types to see how this affects scaling.
    /// </summary>
    [DataField]
    public float ScalingPower = 1;

    /// <summary>
    /// Defines the scaling behavior for the damage.
    /// </summary>
    [DataField]
    public DeltaPressureDamageScalingType ScalingType = DeltaPressureDamageScalingType.Threshold;
}

/// <summary>
/// An enum that defines how the damage dealt by the <see cref="DeltaPressureComponent"/> scales
/// depending on the pressure experienced by the entity.
/// The scaling is done on the effective pressure, which is the pressure above the minimum pressure.
/// See https://www.desmos.com/calculator/9ctlq3zpnt for a visual representation of the scaling types.
/// </summary>
[Serializable]
public enum DeltaPressureDamageScalingType : byte
{
    /// <summary>
    /// Damage dealt will be constant as long as the minimum values are met.
    /// Scaling power is ignored.
    /// </summary>
    Threshold,

    /// <summary>
    /// Damage dealt will be a linear function.
    /// Scaling power determines the slope of the function.
    /// </summary>
    Linear,

    /// <summary>
    /// Damage dealt will be a logarithmic function.
    /// Scaling power determines the base of the log.
    /// </summary>
    Log,
}
