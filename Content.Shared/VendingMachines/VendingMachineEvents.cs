using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines;

public sealed partial class VendingMachineSelfDispenseEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class RestockDoAfterEvent : SimpleDoAfterEvent;
