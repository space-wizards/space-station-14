using Content.Client.DamageState;
using Content.Shared.Backmen.Blob;
using Content.Shared.Backmen.Blob.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Backmen.Blob;

public sealed class BlobTileSystem : SharedBlobTileSystem
{
    protected override void TryRemove(EntityUid target, EntityUid coreUid, BlobTileComponent tile, BlobCoreComponent core)
    {
        // do nothing on client
    }

    protected override void TryUpgrade(EntityUid target, EntityUid user, EntityUid coreUid, BlobTileComponent tile, BlobCoreComponent core)
    {
        // do nothing on client
    }
}

public sealed class BlobTileVisualizerSystem : VisualizerSystem<BlobTileComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlobTileComponent, AfterAutoHandleStateEvent>(OnBlobTileHandleState);
    }

    private void UpdateAppearance(EntityUid id, BlobTileComponent tile, AppearanceComponent? appearance = null, SpriteComponent? sprite = null)
    {
        if (!Resolve(id, ref appearance, ref sprite))
            return;

        foreach (var key in new []{ DamageStateVisualLayers.Base, DamageStateVisualLayers.BaseUnshaded })
        {
            if (!sprite.LayerMapTryGet(key, out _))
                continue;

            sprite.LayerSetColor(key, tile.Color);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, BlobTileComponent component, ref AppearanceChangeEvent args)
    {
        UpdateAppearance(uid, component, args.Component, args.Sprite);
    }

    private void OnBlobTileHandleState(EntityUid uid, BlobTileComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(uid, component);
    }
}
