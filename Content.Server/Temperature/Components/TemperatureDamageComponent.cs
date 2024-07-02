using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Temperature.Components;

/// <summary>
/// Makes the associated entity capable of taking damage in response to its temperature.
/// </summary>
[RegisterComponent]
public sealed partial class TemperatureDamageThresholdsComponent : Component
{
    /// <summary>
    /// The kind and amount of damage that should be applied to the associated entity when it is cold. Scales with temperature.
    /// </summary>
    [DataField]
    public DamageSpecifier ColdDamage = new();

    /// <summary>
    /// The kind and amount of damage that should be applied to the associated entity when it is hot. Scales with temperature.
    /// </summary>
    [DataField]
    public DamageSpecifier HeatDamage = new();

    /// <summary>
    /// The temperature below which the associated entity will start taking <see cref="ColdDamage"/>.
    /// </summary>
    [DataField]
    public float ColdDamageThreshold = 260f;

    /// <summary>
    /// The temperature above which the associated entity will start taking <see cref="HeatDamage"/>.
    /// </summary>
    [DataField]
    public float HeatDamageThreshold = 360f;

    /// <summary>
    /// Overrides <see cref="HeatDamageThreshold"/> if the entity's within a parent with the <see cref="ContainerTemperatureDamageThresholdsComponent"/> component.
    /// </summary>
    [DataField]
    public float? ParentHeatDamageThreshold;

    /// <summary>
    /// Overrides <see cref="ColdDamageThreshold"/> if the entity's within a parent with the <see cref="ContainerTemperatureDamageThresholdsComponent"/> component.
    /// </summary>
    [DataField]
    public float? ParentColdDamageThreshold;

    /// <summary>
    /// Temperature won't do more than this multiple of <see cref="ColdDamage"/> or <see cref="HeatDamage"/> per second.
    /// </summary>
    /// <remarks>
    /// Okay it genuinely reaches this basically immediately for a plasma fire.
    /// </remarks>
    [DataField]
    public FixedPoint2 DamageCap = FixedPoint2.New(8);

    /// <summary>
    /// Used to keep track of when damage starts/stops. Useful for logs.
    /// </summary>
    [DataField]
    public bool TakingDamage = false;

    /// <summary>
    /// The id of the alert presented to the player controlling this entity when it overheats.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> HotAlert = "Hot";

    /// <summary>
    /// The id of the alert presented to the player controlling this entity when it cools too far.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> ColdAlert = "Cold";
}
