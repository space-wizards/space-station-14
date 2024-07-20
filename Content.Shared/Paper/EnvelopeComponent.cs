using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class EnvelopeComponent : Component
{
    [ViewVariables, DataField, AutoNetworkedField]
    public EnvelopeState State = EnvelopeState.Open;

    [DataField, ViewVariables]
    public string SlotId = "letter_slot";

    [DataField, ViewVariables]
    public DoAfterId? EnvelopeDoAfter;

    [DataField, ViewVariables]
    public TimeSpan SealDelay = TimeSpan.FromSeconds(1);

    [DataField, ViewVariables]
    public TimeSpan TearDelay = TimeSpan.FromSeconds(1);

    [DataField, ViewVariables]
    public SoundPathSpecifier SealSound = new SoundPathSpecifier("/Audio/Effects/packetrip.ogg");

    [DataField, ViewVariables]
    public SoundPathSpecifier TearSound = new SoundPathSpecifier("/Audio/Effects/poster_broken.ogg");

[Serializable, NetSerializable]
    public enum EnvelopeState : byte
    {
        Open,
        Sealed,
        Torn
    }
}

[Serializable, NetSerializable]
public sealed partial class EnvelopeDoAfterEvent : SimpleDoAfterEvent
{
}
