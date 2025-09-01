using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Actions.Storage;

[Serializable, NetSerializable]
public sealed partial class PrivateStorageDoAfterEvent : SimpleDoAfterEvent
{
}
