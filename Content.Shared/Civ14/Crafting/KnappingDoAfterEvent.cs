using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Rocks;

[Serializable, NetSerializable]
public sealed partial class KnappingDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}