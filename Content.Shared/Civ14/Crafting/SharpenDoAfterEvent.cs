using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Crafting;

[Serializable, NetSerializable]
public sealed partial class SharpenDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}