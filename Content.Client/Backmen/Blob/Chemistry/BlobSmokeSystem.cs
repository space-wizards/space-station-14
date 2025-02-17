using System.Linq;
using Content.Shared.Backmen.Blob.Chemistry;
using Content.Shared.Chemistry.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Backmen.Blob.Chemistry;

public sealed class BlobSmokeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlobSmokeColorComponent, AfterAutoHandleStateEvent>(OnBlobTileHandleState);
    }

    private void OnBlobTileHandleState(EntityUid uid, BlobSmokeColorComponent component, ref AfterAutoHandleStateEvent state)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            sprite.LayerSetColor(i, component.Color);
        }
    }
}
