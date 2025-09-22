using Content.Shared.Silicons.Bots;
using Content.Shared.Silicons.Bots.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Silicons.Bots;

public sealed class SecuritronVisualsSystem : VisualizerSystem<SecuritronComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, SecuritronComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, SecuritronVisuals.State, out SecuritronVisualState state, args.Component))
            state = SecuritronVisualState.Online;

        var stateName = state == SecuritronVisualState.Combat
            ? component.CombatState
            : component.OnlineState;

        if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), component.BaseLayer, out var layer, false))
            layer = SpriteSystem.LayerMapReserve((uid, args.Sprite), component.BaseLayer);

        args.Sprite.LayerSetState(layer, stateName);
    }
}
