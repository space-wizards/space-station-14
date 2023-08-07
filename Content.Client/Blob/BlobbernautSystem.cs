using System.Linq;
using Content.Client.DamageState;
using Content.Shared.Blob;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Blob;

public sealed class BlobbernautSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlobbernautComponent, ComponentHandleState>(OnBlobTileHandleState);
    }

    private void OnBlobTileHandleState(EntityUid uid, BlobbernautComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BlobbernautComponentState state)
            return;

        if (component.Color == state.Color)
            return;

        component.Color = state.Color;
        TryComp<SpriteComponent>(uid, out var sprite);

        if (sprite == null)
            return;

        foreach (var key in new []{ DamageStateVisualLayers.Base })
        {
            if (!sprite.LayerMapTryGet(key, out _))
                continue;

            sprite.LayerSetColor(key, component.Color);
        }
    }
}
