using Content.Shared.Inventory;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat;

/// <summary>
/// Similar to <seealso cref="TransformSpeakerNameEvent"/>, but for changing the speech
/// sounds of a speaking entity.
/// </summary>
public sealed class TransformSpeakerVoiceEvent(EntityUid sender) : EntityEventArgs, IInventoryRelayEvent
{
    /// <summary>
    /// What slots to look at, slots not included are ignored.
    /// </summary>
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;

    /// <summary>
    /// The entity speaking.
    /// </summary>
    public EntityUid Sender = sender;

    /// <summary>
    /// What speech sounds (if any) to transform into.
    /// </summary>
    public ProtoId<SpeechSoundsPrototype>? SpeechSounds;
}
