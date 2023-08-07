using Content.Client.DamageState;
using Content.Shared.Blob;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Blob;

public sealed class BlobTileSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlobTileComponent, ComponentHandleState>(OnBlobTileHandleState);
    }

    private void OnBlobTileHandleState(EntityUid uid, BlobTileComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BlobTileComponentState state)
            return;

        if (component.Color == state.Color)
            return;

        component.Color = state.Color;
        TryComp<SpriteComponent>(uid, out var sprite);

        if (sprite == null)
            return;

        foreach (var key in new []{ DamageStateVisualLayers.Base, DamageStateVisualLayers.BaseUnshaded })
        {
            if (!sprite.LayerMapTryGet(key, out _))
                continue;

            sprite.LayerSetColor(key, component.Color);
        }
    }
}
