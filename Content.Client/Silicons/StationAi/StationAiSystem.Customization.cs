using Content.Shared.Silicons.StationAi;
using Robust.Client.GameObjects;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    private void InitializeCustomization()
    {
        SubscribeLocalEvent<StationAiCoreComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<StationAiCoreComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<PrototypeLayerData>(entity.Owner, StationAiVisualState.Key, out var layerData, args.Component))
            args.Sprite.LayerSetData(StationAiVisualState.Key, layerData);

        args.Sprite.LayerSetVisible(StationAiVisualState.Key, layerData != null);
    }
}
