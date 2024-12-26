using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Singularity.Systems;

public sealed class EmitterSystem : SharedEmitterSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EmitterComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, EmitterComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<EmitterVisualState>(uid, EmitterVisuals.VisualState, out var state, args.Component))
            state = EmitterVisualState.Off;

        if (!args.Sprite.LayerMapTryGet(EmitterVisualLayers.Lights, out var layer))
            return;

        switch (state)
        {
            case EmitterVisualState.On:
                if (component.OnState == null)
                    break;
                args.Sprite.LayerSetVisible(layer, true);
                args.Sprite.LayerSetState(layer, component.OnState);
                break;
            case EmitterVisualState.Underpowered:
                if (component.UnderpoweredState == null)
                    break;
                args.Sprite.LayerSetVisible(layer, true);
                args.Sprite.LayerSetState(layer, component.UnderpoweredState);
                break;
            case EmitterVisualState.Off:
                args.Sprite.LayerSetVisible(layer, false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
