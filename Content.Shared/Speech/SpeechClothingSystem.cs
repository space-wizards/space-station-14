using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Speech;

/// <summary>
/// This handles the way clothing can change the wearer's voice.
/// </summary>
public sealed class SpeechClothingSystem : EntitySystem
{
    private const string Fallback = "Alto";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpeechClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SpeechClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid uid, SpeechClothingComponent component, GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing) ||
            !TryComp<SpeechComponent>(args.Equipee, out var speech))
            return;

        var isCorrectSlot = clothing.Slots.HasFlag(args.SlotFlags);
        if (!isCorrectSlot)
            return;

        var previousSpeechSound = speech.SpeechSounds ?? Fallback;
        component.Previous = previousSpeechSound;

        speech.SpeechSounds = component.Prototype;
    }

    private void OnGotUnequipped(EntityUid uid, SpeechClothingComponent component, GotUnequippedEvent args)
    {
        if (!TryComp<SpeechComponent>(args.Equipee, out var speech))
            return;

        speech.SpeechSounds = component.Previous;
    }
}
