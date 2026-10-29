using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage;

[Serializable, NetSerializable]
public sealed partial class MagnetPickupDoAfterEvent : SimpleDoAfterEvent;
