using Content.Server._Impstation.Speech.Components;
using Content.Server.VoiceMask;
using Content.Shared.Chat;
using Content.Shared.Inventory;

namespace Content.Server._Impstation.Speech.EntitySystems;

public sealed partial class SpeechSoundSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeechSoundComponent, InventoryRelayedEvent<TransformSpeakerVoiceEvent>>(OnTransformVoice);
        SubscribeLocalEvent<SpeechSoundComponent, InventoryRelayedEvent<TransformSpeakerNameEvent>>(OnTransformName);
    }

    private void OnTransformVoice(Entity<SpeechSoundComponent> ent, ref InventoryRelayedEvent<TransformSpeakerVoiceEvent> args)
    {
        args.Args.SpeechSounds = ent.Comp.SpeechSounds ?? args.Args.SpeechSounds;
    }

    private void OnTransformName(Entity<SpeechSoundComponent> ent, ref InventoryRelayedEvent<TransformSpeakerNameEvent> args)
    {
        args.Args.SpeechVerb = ent.Comp.SpeechVerb ?? args.Args.SpeechVerb;
    }
}
