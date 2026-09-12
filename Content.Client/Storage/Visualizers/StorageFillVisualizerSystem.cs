using Content.Shared.Storage.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Visualizers;

public sealed partial class StorageFillVisualizerSystem : VisualizerSystem<StorageFillVisualizerComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, StorageFillVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.TryGetData<int>(StorageFillVisuals.FillLevel, out var level))
            return;

        var state = $"{component.FillBaseName}-{level}";
        SpriteSystem.LayerSetRsiState((uid, args.Sprite), StorageFillLayers.Fill, state);
    }
}
