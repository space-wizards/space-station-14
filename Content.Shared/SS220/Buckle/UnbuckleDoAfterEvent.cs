// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Buckle;

[Serializable, NetSerializable]
public sealed partial class UnbuckleDoAfterEvent : SimpleDoAfterEvent
{
}
