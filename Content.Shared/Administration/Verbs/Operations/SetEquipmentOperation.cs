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

    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear { get; private set; }

    [DataField]
    public bool ClearOtherSlots { get; private set; }

    [DataField]
    public bool Unremoveable { get; private set; }
}
