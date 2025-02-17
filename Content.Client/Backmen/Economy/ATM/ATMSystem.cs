// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Backmen.Economy.ATM;
using Robust.Client.GameObjects;

namespace Content.Client.Backmen.Economy.ATM;

public sealed class ATMSystem : SharedATMSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AtmComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, AtmComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(ATMVisuals.VisualState, out var visualStateObject) ||
            visualStateObject is not ATMVisualState visualState)
        {
            visualState = ATMVisualState.Normal;
        }

        UpdateAppearance(uid, visualState, component, args.Sprite);
    }

    private void UpdateAppearance(EntityUid uid, ATMVisualState visualState, AtmComponent component, SpriteComponent sprite)
    {
        SetLayerState(ATMVisualLayers.Base, component.OffState, sprite);

        switch (visualState)
        {
            case ATMVisualState.Normal:
                SetLayerState(ATMVisualLayers.BaseUnshaded, component.NormalState, sprite);
                break;
            case ATMVisualState.Off:
                if (!sprite.LayerMapTryGet(ATMVisualLayers.BaseUnshaded, out var actualLayer))
                    break;
                sprite.LayerSetVisible(actualLayer, false);
                break;
        }
    }

    private static void SetLayerState(ATMVisualLayers layer, string? state, SpriteComponent sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;
        sprite.LayerSetVisible(layer, true);
        sprite.LayerSetState(layer, state);
    }
}

public enum ATMVisualLayers : byte
{
    Base,
    BaseUnshaded
}
