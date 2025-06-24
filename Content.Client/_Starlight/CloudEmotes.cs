using System.Numerics;
using Content.Shared._Starlight.CloudEmotes;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Starlight;

public sealed class ClouldEmotesSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private SpriteSpecifier EmoteStart = new SpriteSpecifier.Rsi(new ResPath("_Starlight/Effects/cloud_emotes.rsi"), "emote_start");
    private SpriteSpecifier EmoteEnd = new SpriteSpecifier.Rsi(new ResPath("_Starlight/Effects/cloud_emotes.rsi"), "emote_end");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CloudEmotesMessage>(OnMessage);
    }

    private void OnMessage(CloudEmotesMessage args, EntitySessionEventArgs session)
    {
        Emote(GetEntity(args.Uid), args.Emote);
    }

    public void Emote(EntityUid uid, string emote)
    {
        if (!_prototypeManager.TryIndex<CloudEmotePrototype>(emote, out var cloudEmote))
            return;
            
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), CloudEmotesKey.Key, out var _, false))
            return;

        var adj = _sprite.GetLocalBounds((uid, sprite)).Height / 2 + ((1.0f / 32) * 6.0f);

        var layer = _sprite.AddLayer((uid, sprite), EmoteStart);
        _sprite.LayerMapSet((uid, sprite), CloudEmotesKey.Key, layer);

        _sprite.LayerSetOffset((uid, sprite), layer, new Vector2(0.0f, adj));
        sprite.LayerSetShader(layer, "unshaded");

        _audio.PlayEntity(cloudEmote.Sound, Filter.Local(), uid, true);

        Timer.Spawn(TimeSpan.FromSeconds(0.2f), () => _sprite.LayerSetSprite((uid, sprite), CloudEmotesKey.Key, cloudEmote.Icon));

        Timer.Spawn(TimeSpan.FromSeconds(0.2f + cloudEmote.AnimationTime), () => _sprite.LayerSetSprite((uid, sprite), CloudEmotesKey.Key, EmoteEnd));

        Timer.Spawn(TimeSpan.FromSeconds(0.4f + cloudEmote.AnimationTime), () => _sprite.RemoveLayer((uid, sprite), CloudEmotesKey.Key));
    }

    private enum CloudEmotesKey
    {
        Key,
    }
}
