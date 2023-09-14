using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared.UserInterface;

[UsedImplicitly]
public sealed partial class ToggleIntrinsicUIEvent : InstantActionEvent
{
    [DataField("key", customTypeSerializer: typeof(EnumSerializer), required: true)]
    public Enum? Key { get; set; }
}
