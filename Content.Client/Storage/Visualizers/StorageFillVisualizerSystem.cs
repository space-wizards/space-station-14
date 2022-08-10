using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Visualizers;

public sealed class StorageFillVisualizerSystem : VisualizerSystem<StorageFillVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, StorageFillVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (!args.Component.TryGetData(StorageFillVisuals.FillLevel, out int level))
            return;

        var state = $"{component.FillBaseName}-{level}";
        args.Sprite.LayerSetState(StorageFillLayers.Fill, state);
    }
}
