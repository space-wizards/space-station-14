using Content.Shared.Chat;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class VoiceOverrideSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoiceOverrideComponent, TransformSpeakerNameEvent>(OnTransformSpeakerName);
    }

    private void OnTransformSpeakerName(Entity<VoiceOverrideComponent> entity, ref TransformSpeakerNameEvent args)
    {
        if (!entity.Comp.Enabled)
            return;

        if (entity.Comp.NameOverride != null)
            args.VoiceName = Loc.GetString(entity.Comp.NameOverride);

        args.SpeechVerb = entity.Comp.SpeechVerbOverride ?? args.SpeechVerb;
    }
}
