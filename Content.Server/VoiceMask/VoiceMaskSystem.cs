using Content.Server.Chat.Systems;
using Content.Shared.IdentityManagement;

namespace Content.Server.VoiceMask;

public sealed class VoiceMaskSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<VoiceMaskComponent, TransformSpeakerNameEvent>(OnSpeakerNameTransform);
    }

    private void OnSpeakerNameTransform(EntityUid uid, VoiceMaskComponent component, TransformSpeakerNameEvent args)
    {
        if (component.Enabled)
        {
            args.Name = Identity.Name(uid, EntityManager);
        }
    }
}
