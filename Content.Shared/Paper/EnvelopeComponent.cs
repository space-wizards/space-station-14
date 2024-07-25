using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class EnvelopeComponent : Component
{
    /// <summary>
    /// The current open/sealed/torn state of the envelope
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public EnvelopeState State = EnvelopeState.Open;

    [DataField, ViewVariables]
    public string SlotId = "letter_slot";

    /// <summary>
    /// Stores the current sealing/tearing doafter of the envelope
    /// to prevent doafter spam/prediction issues
    /// </summary>
    [DataField, ViewVariables]
    public DoAfterId? EnvelopeDoAfter;

    /// <summary>
    /// How long it takes to seal the envelope closed
    /// </summary>
    [DataField, ViewVariables]
    public TimeSpan SealDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long it takes to tear open the envelope
    /// </summary>
    [DataField, ViewVariables]
    public TimeSpan TearDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The sound to play when the envelope is sealed closed
    /// </summary>
    [DataField, ViewVariables]
    public SoundPathSpecifier? SealSound = new SoundPathSpecifier("/Audio/Effects/packetrip.ogg");

    /// <summary>
    /// The sound to play when the envelope is torn open
    /// </summary>
    [DataField, ViewVariables]
    public SoundPathSpecifier? TearSound = new SoundPathSpecifier("/Audio/Effects/poster_broken.ogg");

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
