using Content.Shared.Chat;
using Robust.Shared.Containers;
using Content.Shared.Whitelist;
using Content.Shared.Speech;

namespace Content.Shared.ChangeNameInContainer;

public sealed partial class ChangeNameInContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangeVoiceInContainerComponent, TransformSpeakerNameEvent>(OnTransformSpeakerName);
    }

    private void OnTransformSpeakerName(Entity<ChangeVoiceInContainerComponent> ent, ref TransformSpeakerNameEvent args)
    {
        if (!_container.TryGetContainingContainer((ent, null, null), out var container)
            || _whitelist.IsWhitelistFail(ent.Comp.Whitelist, container.Owner))
            return;

        args.VoiceName = Name(container.Owner);
        if (TryComp<SpeechComponent>(container.Owner, out var speechComp))
            args.SpeechVerb = speechComp.SpeechVerb;
    }

}
