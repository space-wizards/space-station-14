// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.LinkedToggleable;

[RegisterComponent]
public sealed partial class LinkedToggleableComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("state")]
    public bool State;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("toggled")]
    public bool Toggled;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("requirePower")]
    public bool RequirePower;

    [DataField("onPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string OnPort = "On";

    [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string OffPort = "Off";

    [DataField("togglePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string TogglePort = "Toggle";
}

[Serializable, NetSerializable]
public enum LinkedToggleableVisuals
{
    StateLayer,
    State
}
