using System.Linq;
using Content.Shared.Chemistry.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Chemistry.Components;

public sealed class SmokeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SmokeComponent, ComponentHandleState>(OnBlobTileHandleState);
    }

    private void OnBlobTileHandleState(EntityUid uid, SmokeComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SmokeComponentState state)
            return;

        if (component.Color == state.Color)
            return;

        component.Color = state.Color;
        TryComp<SpriteComponent>(uid, out var sprite);

        if (sprite == null)
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            sprite.LayerSetColor(i, component.Color);
        }
    }
}
