using Robust.Shared.GameStates;
using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Emoting;
using Robust.Shared.Prototypes;

namespace Content.Server.Emoting;

public sealed partial class AnimatedEmotesSystem : SharedAnimatedEmotesSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimatedEmotesComponent, EmoteEvent>(OnEmote);
    }

    private void OnEmote(EntityUid uid, AnimatedEmotesComponent component, ref EmoteEvent args)
    {
        PlayEmoteAnimation(uid, component, args.Emote.ID);
    }

    public void PlayEmoteAnimation(EntityUid uid, AnimatedEmotesComponent component, ProtoId<EmotePrototype> prot)
    {
        component.Emote = prot;
        Dirty(uid, component);
    }
}
