using System.Numerics;
using Content.Shared._Starlight.CloudEmotes;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Starlight;

public sealed class ClouldEmotesSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

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

        var layer = _sprite.AddLayer((uid, sprite), cloudEmote.Icon);
        _sprite.LayerMapSet((uid, sprite), CloudEmotesKey.Key, layer);

        _sprite.LayerSetOffset((uid, sprite), layer, new Vector2(0.0f, adj));
        sprite.LayerSetShader(layer, "unshaded");

        _audio.PlayEntity(cloudEmote.Sound, Filter.Local(), uid, true);

        Timer.Spawn(TimeSpan.FromSeconds(cloudEmote.AnimationTime), () => _sprite.RemoveLayer((uid, sprite), layer));
    }

    private enum CloudEmotesKey
    {
        Key,
    }
}
