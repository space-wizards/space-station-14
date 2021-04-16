#nullable enable
using Content.Shared.GameObjects.Components.Storage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using System;

namespace Content.Client.GameObjects.Components.Storage
{
    /// <summary>
    ///     Visualize storage filling level
    ///     Need ServerStorageComponent.showFillLevel to work properly
    /// </summary>
    [UsedImplicitly]
    public class StorageFillVisualizer : AppearanceVisualizer
    {
        [DataField("maxFillLevels")] private int _maxFillLevels = 0;
        [DataField("fillBaseName")] private string? _fillBaseName = null;
        [DataField("layer")] private StorageVisualLayers _layer = StorageVisualLayers.FillLevel;

        public override void OnChangeData(AppearanceComponent component)
        {
            if (_maxFillLevels <= 0 || _fillBaseName == null) return;

            if (!component.TryGetData(StorageVisuals.FillLevel,
                out StorageFillLevel state)) return;

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite)) return;
            if (!sprite.LayerMapTryGet(_layer, out var fillLayer)) return;

            var fillPercent = (float) state.StorageSizeUsed / state.StorageSizeMax;
            var closestFillSprite = (int) Math.Round(fillPercent * _maxFillLevels);

            var stateName = _fillBaseName + closestFillSprite;
            sprite.LayerSetState(fillLayer, stateName);
        }
    }
}
