using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Handles taking damage from being excessively hot/cold.
/// Also handles alerts about being too hot or too cold.
/// </summary>
[RegisterComponent]
public sealed partial class TemperatureDamageComponent : Component
{
    /// <summary>
    /// The temperature above which the entity will start taking damage from being too hot.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HeatDamageThreshold = 360f;

    /// <summary>
    /// The temperature below which the entity will start taking damage from being too cold.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ColdDamageThreshold = 260f;

    /// <summary>
    /// Overrides HeatDamageThreshold if the entity's within a parent with the ContainerTemperatureDamageThresholdsComponent component.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float? ParentHeatDamageThreshold;

    /// <summary>
    /// Overrides ColdDamageThreshold if the entity's within a parent with the ContainerTemperatureDamageThresholdsComponent component.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float? ParentColdDamageThreshold;

    /// <summary>
    /// The base damage that this entity will take if it's too cold.
    /// Will be scaled according to how cold it is.
    /// The scaling maxes out at <see cref="DamageCap"/> times this damage per second.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier ColdDamage = new();

    /// <summary>
    /// The base damage that this entity will take per second if it's too hot.
    /// Will be scaled according to how hot it is.
    /// The scaling maxes out at <see cref="DamageCap"/> times this damage per second.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier HeatDamage = new();

    /// <summary>
    /// Temperature won't do more than this multiple of the base overheating/overcooling damage per seond.
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

    /// <summary>
    /// The id of the alert thrown when the entity is too hot.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> HotAlert = "Hot";

    /// <summary>
    /// The id of the alert thrown when the entity is too cold.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> ColdAlert = "Cold";

    /// <summary>
    /// The last time this entity processed temperature damage.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastUpdate;

    /// <summary>
    /// The time interval between temperature damage ticks for this entity.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1.0);
}
