using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Equips optional starting gear, then overrides configured slots with explicit equipment.
/// </summary>
public sealed partial class SetEquipmentOperation : AdminOperationBase<SetEquipmentOperation>
{
    [DataField]
    public Dictionary<string, EntProtoId> Equipment { get; private set; } = new();

    /// <summary>
    /// Applied before <see cref="Equipment"/>; explicit entries replace gear in the same slots.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear { get; private set; }

    /// <summary>
    /// With no starting gear, clears every slot before equipping explicit entries.
    /// Starting gear always uses its normal outfit replacement behavior.
    /// </summary>
    [DataField]
    public bool ClearOtherSlots { get; private set; }

    /// <summary>
    /// Adds <c>UnremoveableComponent</c> to clothing equipped by this operation.
    /// </summary>
    [DataField]
    public bool Unremoveable { get; private set; }
}
