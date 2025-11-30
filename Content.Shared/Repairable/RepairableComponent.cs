using Content.Shared.Damage;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Repairable;

/// <summary>
/// Use this component to mark a device as repairable.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RepairableComponent : Component
{
    /// <summary>
    ///     All the damage to change information is stored in this <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     If this data-field is specified, it will change damage by this amount instead of setting all damage to 0.
    ///     in order to heal/repair the damage values have to be negative.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public DamageSpecifier? Damage;

    /// <summary>
    /// The damage change when the entity is not alive (if they can be critted or die)
    /// If null then it will use the <see cref="Damage"> for every situation
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier? DamageCrit;

    /// <summary>
    /// Cost of fuel used to repair this device.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FuelCost = 5f;

    /// <summary>
    /// Tool quality necessary to repair this device.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> QualityNeeded = "Welding";

    /// <summary>
    /// The base tool use delay (seconds). This will be modified by the tool's quality
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DoAfterDelay = 1;

    /// <summary>
    /// If after the doafter ends it should start a new one
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoDoAfter = false;

    /// <summary>
    /// A multiplier that will be applied to the above if an entity is repairing themselves.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SelfRepairPenalty = 3f;

    /// <summary>
    /// Whether an entity is allowed to repair itself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowSelfRepair = true;
}
