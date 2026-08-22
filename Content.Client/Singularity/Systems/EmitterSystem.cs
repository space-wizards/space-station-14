using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Singularity.Systems;

public sealed partial class EmitterSystem : SharedEmitterSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<EmitterComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.TryGetData<EmitterVisualState>(EmitterVisuals.VisualState, out var state))
            state = EmitterVisualState.Off;

        if (!_sprite.LayerMapTryGet((ent, args.Sprite), EmitterVisualLayers.Lights, out var layer, false))
            return;

        switch (state)
        {
            case EmitterVisualState.On:
                if (ent.Comp.OnState == null)
                    break;
                _sprite.LayerSetVisible((ent, args.Sprite), layer, true);
                _sprite.LayerSetRsiState((ent, args.Sprite), layer, ent.Comp.OnState);
                break;
            case EmitterVisualState.Underpowered:
                if (ent.Comp.UnderpoweredState == null)
                    break;
                _sprite.LayerSetVisible((ent, args.Sprite), layer, true);
                _sprite.LayerSetRsiState((ent, args.Sprite), layer, ent.Comp.UnderpoweredState);
                break;
            case EmitterVisualState.Off:
                _sprite.LayerSetVisible((ent, args.Sprite), layer, false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
