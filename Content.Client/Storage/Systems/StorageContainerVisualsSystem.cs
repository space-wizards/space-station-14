using Content.Client.Storage.Components;
using Content.Shared.Rounding;
using Content.Shared.Storage;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Systems;

/// <inheritdoc cref="StorageContainerVisualsComponent"/>
public sealed partial class StorageContainerVisualsSystem : VisualizerSystem<StorageContainerVisualsComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, StorageContainerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.TryGetData<int>(StorageVisuals.StorageUsed, out var used))
            return;

        if (!args.TryGetData<int>(StorageVisuals.Capacity, out var capacity))
            return;

        var fraction = used / (float)capacity;

        if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), component.FillLayer, out var fillLayer, false))
            return;

        var closestFillSprite = Math.Min(ContentHelpers.RoundToNearestLevels(fraction, 1, component.MaxFillLevels + 1),
            component.MaxFillLevels);

        if (closestFillSprite > 0)
        {
            if (component.FillBaseName == null)
                return;

            SpriteSystem.LayerSetVisible((uid, args.Sprite), fillLayer, true);
            var stateName = component.FillBaseName + closestFillSprite;
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), fillLayer, stateName);
        }
        else
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), fillLayer, false);
        }
    }
}
