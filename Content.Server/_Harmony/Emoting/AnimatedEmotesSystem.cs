// Original code by whateverusername0 from Goob-Station at commit 3022db4
// Available at: https://github.com/Goob-Station/Goob-Station/blob/3022db48e89ff00b762004767e7850023df3ee97/Content.Server/_Goobstation/Emoting/AnimatedEmotesSystem.cs

using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Content.Shared._Harmony.Emoting;

namespace Content.Server._Harmony.Emoting;

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
