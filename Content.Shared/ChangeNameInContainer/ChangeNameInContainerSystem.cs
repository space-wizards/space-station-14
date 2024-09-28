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
        SubscribeLocalEvent<ChangeVoiceInContainerComponent, TransformSpeakerNameEvent>(UpdateName);
    }

    private void UpdateName(Entity<ChangeVoiceInContainerComponent> entity, ref TransformSpeakerNameEvent args)
    {
        if (!_container.TryGetContainingContainer((entity, null, null), out var container)
            || _whitelist.IsWhitelistFailOrNull(entity.Comp.Whitelist, container.Owner))
            return;

        args.VoiceName = MetaData(container.Owner).EntityName;
        if (TryComp<SpeechComponent>(container.Owner, out var speechComp))
            args.SpeechVerb = speechComp.SpeechVerb;
    }

}
