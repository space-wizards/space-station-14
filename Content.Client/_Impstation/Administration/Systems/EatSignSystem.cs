using System.Numerics;
using Content.Client._Impstation.Administration.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Impstation.Administration.Systems;

public sealed class EatSignSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<EatSignComponent, ComponentStartup>(EatSignAdded);
        SubscribeLocalEvent<EatSignComponent, ComponentShutdown>(EatSignRemoved);
    }

    private void EatSignRemoved(EntityUid uid, EatSignComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!sprite.LayerMapTryGet(EatSignKey.Key, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }

    private void EatSignAdded(EntityUid uid, EatSignComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (sprite.LayerMapTryGet(EatSignKey.Key, out var _))
            return;

        var adj = sprite.Bounds.Height - 0.6f;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(new ResPath("_Impstation/Objects/Misc/eatsign.rsi"), "sign"));
        sprite.LayerMapSet(EatSignKey.Key, layer);

        sprite.LayerSetOffset(layer, new Vector2(0.0f, adj));
        sprite.LayerSetShader(layer, "unshaded");
    }

    private enum EatSignKey
    {
        Key,
    }
}
