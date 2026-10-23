using Content.Server._MACRO.Speech.Components;
using Content.Shared.Chat;
using Content.Shared.Inventory;

namespace Content.Server._MACRO.Speech.EntitySystems;

public sealed class SpeechSoundSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeechSoundComponent, InventoryRelayedEvent<TransformSpeakerVoiceEvent>>(OnTransformVoice);
        SubscribeLocalEvent<SpeechSoundComponent, InventoryRelayedEvent<TransformSpeakerNameEvent>>(OnTransformName);
    }

    private static void OnTransformVoice(Entity<SpeechSoundComponent> ent, ref InventoryRelayedEvent<TransformSpeakerVoiceEvent> args)
    {
        args.Args.SpeechSounds = ent.Comp.SpeechSounds ?? args.Args.SpeechSounds;
    }

    private static void OnTransformName(Entity<SpeechSoundComponent> ent, ref InventoryRelayedEvent<TransformSpeakerNameEvent> args)
    {
        args.Args.SpeechVerb = ent.Comp.SpeechVerb ?? args.Args.SpeechVerb;
    }
}
