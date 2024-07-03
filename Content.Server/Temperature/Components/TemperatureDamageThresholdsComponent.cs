using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Temperature.Components;

/// <summary>
/// Handles taking overheating/overcooling damage.
/// </summary>
[RegisterComponent]
public sealed partial class TemperatureDamageThresholdsComponent : Component
{
    /// <summary>
    /// The temperature above which the entity begins to overheat and take damage.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HeatDamageThreshold = 360f;

    /// <summary>
    /// The temperature below which the entity begins to overcool and take damage.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ColdDamageThreshold = 260f;

    /// <summary>
    /// Overrides <see cref="HeatDamageThreshold"/> if the entity's within a parent with the <see cref="TemperatureDamageThresholdsComponent"/> component.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float? ParentHeatDamageThreshold;

    /// <summary>
    /// Overrides <see cref="ColdDamageThreshold"/> if the entity's within a parent with the <see cref="TemperatureDamageThresholdsComponent"/> component.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float? ParentColdDamageThreshold;

    /// <summary>
    /// The base amount of damage this entity takes per second when overcooling, scaled by the degree to which it is overcooling.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier ColdDamage = new();

    /// <summary>
    /// The base amount of damage this entity takes per second when overheating, scaled by the degree to which it is overheating.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier HeatDamage = new();

    /// <summary>
    /// The maximum multiple of the base damage this entity will take per second when overheating/overcooling.
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

    /// <summary>
    /// The alert thrown when the entity overheats.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> HotAlert = "Hot";

    /// <summary>
    /// The alert thrown when the entity overcools.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> ColdAlert = "Cold";
}
