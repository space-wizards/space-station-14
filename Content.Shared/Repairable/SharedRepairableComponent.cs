using Content.Shared.Damage;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Repairable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SharedRepairableComponent : Component
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

    [DataField]
    public int FuelCost = 5;

    [DataField]
    public ProtoId<ToolQualityPrototype> QualityNeeded = "Welding";

    [DataField]
    public int DoAfterDelay = 1;

    /// <summary>
    /// A multiplier that will be applied to the above if an entity is repairing themselves.
    /// </summary>
    [DataField]
    public float SelfRepairPenalty = 3f;

    /// <summary>
    /// Whether or not an entity is allowed to repair itself.
    /// </summary>
    [DataField]
    public bool AllowSelfRepair = true;
}
