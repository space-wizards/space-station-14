using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Blob;


[Serializable, NetSerializable]
public sealed class BlobPodZombifyDoAfterEvent : SimpleDoAfterEvent
{
}
