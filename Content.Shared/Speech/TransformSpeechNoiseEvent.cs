using Robust.Shared.Prototypes;
using Content.Shared.Inventory;

namespace Content.Shared.Speech;

/// <summary>
///     This event is sent when about to speak and will get any override values for the speech noise.
///     It is an inventory relay event so will be relayed to clothing as well.
/// </summary>
public sealed class TransformSpeechNoiseEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public EntityUid Speaker;
    public ProtoId<SpeechSoundsPrototype>? OverrideSpeechSound;

    public TransformSpeechNoiseEvent(EntityUid speaker)
    {
        Speaker = speaker;
        OverrideSpeechSound = null;
    }
}
