// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Blinds;

[RegisterComponent]
public sealed class BlindsComponent : Component
{
    [DataField("isOpen"), ViewVariables(VVAccess.ReadOnly)]
    public bool IsOpen = true;

    [DataField("openCloseDuration"), ViewVariables(VVAccess.ReadWrite)]
    public float OpenCloseDuration = 1f;

    [DataField("closedRsiStatePostfix"), ViewVariables(VVAccess.ReadOnly)]
    public string ClosedRsiStatePostfix = "_closed";

    [DataField("openSound")]
    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/SS220/Blinds/open.ogg");

    [DataField("closeSound")]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/SS220/Blinds/close.ogg");
}

[Serializable, NetSerializable]
public enum BlindsVisualState
{
    State,
}
