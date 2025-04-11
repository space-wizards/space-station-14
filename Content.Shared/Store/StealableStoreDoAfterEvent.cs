using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Store;

[Serializable, NetSerializable]
public sealed partial class StealableStoreDoAfterEvent : SimpleDoAfterEvent;
