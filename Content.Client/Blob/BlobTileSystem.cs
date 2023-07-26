using System.Linq;
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

        if (component.State == state.State) return;

        component.State = state.State;
        TryComp<SpriteComponent>(uid, out var sprite);

        if (sprite == null)
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            switch (state.State)
            {
                case BlobTileState.Dead:
                    sprite.LayerSetColor(i, Color.White);
                    break;
                case BlobTileState.Blue:
                    sprite.LayerSetColor(i, Color.Blue);
                    break;
                case BlobTileState.Green:
                    sprite.LayerSetColor(i, Color.Green);
                    break;
            }
        }


    }
}
