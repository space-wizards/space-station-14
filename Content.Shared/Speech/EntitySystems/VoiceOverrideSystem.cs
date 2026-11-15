using Content.Shared.Chat;
using Content.Shared.Speech.Components;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class VoiceOverrideSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnTransformSpeakerName(Entity<VoiceOverrideComponent> ent, ref TransformSpeakerNameEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        args.VoiceName = ent.Comp.NameOverride ?? args.VoiceName;
        args.SpeechVerb = ent.Comp.SpeechVerbOverride ?? args.SpeechVerb;
    }
}
